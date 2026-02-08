using System.Xml.Linq;
using Core.Application;
using Core.Domain;

namespace Core.Infrastructure.ConsoleApp.Services;

/// <summary>
/// Implements <see cref="ICacheManager"/> by scanning the NuGet packages cache for .NET tools.
/// </summary>
public class DotnetCacheManager : ICacheManager
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<InstalledTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = new List<InstalledTool>();
        var cacheDir = GetNuGetPackagesCacheDirectory();

        if (!Directory.Exists(cacheDir))
        {
            return Task.FromResult<IReadOnlyList<InstalledTool>>(tools);
        }

        foreach (var packageDir in Directory.GetDirectories(cacheDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packageId = Path.GetFileName(packageDir);
            var versionDirs = Directory.GetDirectories(packageDir);

            foreach (var versionDir in versionDirs)
            {
                var version = Path.GetFileName(versionDir);

                // Check if this is a .NET tool by looking for DotnetTool package type in nuspec
                if (IsDotnetTool(versionDir))
                {
                    var command = GetToolCommand(versionDir, packageId);
                    tools.Add(new InstalledTool(packageId, version, command));
                }
            }
        }

        // Sort by package ID for consistent output
        tools.Sort((a, b) => string.Compare(a.PackageId, b.PackageId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<IReadOnlyList<InstalledTool>>(tools);
    }

    /// <inheritdoc/>
    public async Task<InstalledTool?> GetToolAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var tools = await ListToolsAsync(cancellationToken);
        return tools.FirstOrDefault(t => t.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InstalledTool>> GetToolVersionsAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var tools = await ListToolsAsync(cancellationToken);
        return tools.Where(t => t.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <inheritdoc/>
    public Task<bool> RemoveToolAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheDir = GetNuGetPackagesCacheDirectory();
            var packageDir = Path.Combine(cacheDir, packageId.ToLowerInvariant());

            if (!Directory.Exists(packageDir))
            {
                return Task.FromResult(false);
            }

            Directory.Delete(packageDir, recursive: true);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<int> ClearAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = await ListToolsAsync(cancellationToken);
        var removedCount = 0;

        // Group by package ID to avoid removing the same package multiple times
        var packageIds = tools.Select(t => t.PackageId).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in packageIds)
        {
            if (await RemoveToolAsync(packageId, cancellationToken))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    /// <inheritdoc/>
    public Task<string?> GetToolExecutablePathAsync(string packageId, string? version = null, CancellationToken cancellationToken = default)
    {
        var cacheDir = GetNuGetPackagesCacheDirectory();
        var packageDir = Path.Combine(cacheDir, packageId.ToLowerInvariant());

        if (!Directory.Exists(packageDir))
        {
            return Task.FromResult<string?>(null);
        }

        // If version not specified, find the latest version
        string? targetVersion = version;
        if (string.IsNullOrEmpty(targetVersion))
        {
            targetVersion = GetLatestCachedVersion(packageDir);
            if (targetVersion == null)
            {
                return Task.FromResult<string?>(null);
            }
        }

        var versionDir = Path.Combine(packageDir, targetVersion);
        if (!Directory.Exists(versionDir))
        {
            return Task.FromResult<string?>(null);
        }

        // Verify this is a .NET tool
        if (!IsDotnetTool(versionDir))
        {
            return Task.FromResult<string?>(null);
        }

        // Find the executable DLL by looking for .runtimeconfig.json
        var executablePath = FindExecutableDll(versionDir);
        return Task.FromResult(executablePath);
    }

    /// <summary>
    /// Gets the latest version from cached version directories.
    /// </summary>
    private static string? GetLatestCachedVersion(string packageDir)
    {
        var versionDirs = Directory.GetDirectories(packageDir);
        if (versionDirs.Length == 0)
        {
            return null;
        }

        // Parse versions and find the latest
        var versions = new List<(string Dir, SemanticVersion? Version)>();
        foreach (var dir in versionDirs)
        {
            var versionString = Path.GetFileName(dir);
            if (SemanticVersion.TryParse(versionString, out var semVer))
            {
                versions.Add((versionString, semVer));
            }
        }

        if (versions.Count == 0)
        {
            return Path.GetFileName(versionDirs[0]);
        }

        return versions.OrderByDescending(v => v.Version).First().Dir;
    }

    /// <summary>
    /// Finds the executable DLL in the tools directory by looking for .runtimeconfig.json files.
    /// </summary>
    private static string? FindExecutableDll(string versionDir)
    {
        var toolsDir = Path.Combine(versionDir, "tools");
        if (!Directory.Exists(toolsDir))
        {
            return null;
        }

        // Search for .runtimeconfig.json files which indicate the entry point
        var runtimeConfigFiles = Directory.GetFiles(toolsDir, "*.runtimeconfig.json", SearchOption.AllDirectories);
        if (runtimeConfigFiles.Length == 0)
        {
            return null;
        }

        // Get the DLL name from the runtimeconfig filename
        var runtimeConfigPath = runtimeConfigFiles[0];
        var dllName = Path.GetFileNameWithoutExtension(runtimeConfigPath).Replace(".runtimeconfig", "") + ".dll";
        var dllPath = Path.Combine(Path.GetDirectoryName(runtimeConfigPath)!, dllName);

        return File.Exists(dllPath) ? dllPath : null;
    }

    /// <summary>
    /// Gets the NuGet packages cache directory path.
    /// </summary>
    /// <returns>The path to the NuGet packages cache.</returns>
    private static string GetNuGetPackagesCacheDirectory()
    {
        // Check NUGET_PACKAGES environment variable first
        var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(nugetPackages))
        {
            return nugetPackages;
        }

        // Default locations:
        // Windows: %USERPROFILE%\.nuget\packages
        // Linux/macOS: ~/.nuget/packages
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
    }

    /// <summary>
    /// Checks if the package is a .NET tool by looking for DotnetTool package type in nuspec.
    /// </summary>
    private static bool IsDotnetTool(string versionDir)
    {
        try
        {
            var nuspecFiles = Directory.GetFiles(versionDir, "*.nuspec");
            if (nuspecFiles.Length == 0)
            {
                return false;
            }

            var nuspec = XDocument.Load(nuspecFiles[0]);
            var ns = nuspec.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var metadata = nuspec.Root?.Element(ns + "metadata");
            var packageTypes = metadata?.Element(ns + "packageTypes");

            if (packageTypes != null)
            {
                foreach (var packageType in packageTypes.Elements(ns + "packageType"))
                {
                    var name = packageType.Attribute("name")?.Value;
                    if (string.Equals(name, "DotnetTool", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the tool command name from the nuspec file.
    /// </summary>
    private static string GetToolCommand(string versionDir, string packageId)
    {
        try
        {
            // Look for nuspec file
            var nuspecFiles = Directory.GetFiles(versionDir, "*.nuspec");
            if (nuspecFiles.Length > 0)
            {
                var nuspec = XDocument.Load(nuspecFiles[0]);
                var ns = nuspec.Root?.GetDefaultNamespace() ?? XNamespace.None;

                // Try to get tool command from metadata
                var metadata = nuspec.Root?.Element(ns + "metadata");
                var toolCommandName = metadata?.Element(ns + "toolCommandName")?.Value;
                if (!string.IsNullOrEmpty(toolCommandName))
                {
                    return toolCommandName;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        // Fallback to package ID as the command name
        return packageId;
    }
}
