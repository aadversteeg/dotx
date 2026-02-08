using System.Xml.Linq;
using Ave.Extensions.FileSystem;
using Core.Application;
using Core.Domain;

namespace Core.Infrastructure.ConsoleApp.Services;

/// <summary>
/// Implements <see cref="ICacheManager"/> by scanning the NuGet packages cache for .NET tools.
/// </summary>
public class DotnetCacheManager : ICacheManager
{
    private readonly IFileSystem _fileSystem;
    private readonly CanonicalPath _cacheDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCacheManager"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system to use for operations.</param>
    /// <param name="cacheDirectory">The root cache directory path.</param>
    public DotnetCacheManager(IFileSystem fileSystem, CanonicalPath cacheDirectory)
    {
        _fileSystem = fileSystem;
        _cacheDirectory = cacheDirectory;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InstalledTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = new List<InstalledTool>();

        var cacheDirExists = await _fileSystem.DirectoryExistsAsync(_cacheDirectory, cancellationToken);
        if (cacheDirExists.IsFailure || !cacheDirExists.Value)
        {
            return tools;
        }

        var packageDirsResult = await _fileSystem.GetDirectoriesAsync(_cacheDirectory, "*", recursive: false, cancellationToken);
        if (packageDirsResult.IsFailure)
        {
            return tools;
        }

        foreach (var packageDir in packageDirsResult.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packageId = packageDir.GetName();

            var versionDirsResult = await _fileSystem.GetDirectoriesAsync(packageDir, "*", recursive: false, cancellationToken);
            if (versionDirsResult.IsFailure)
            {
                continue;
            }

            foreach (var versionDir in versionDirsResult.Value)
            {
                var version = versionDir.GetName();

                // Check if this is a .NET tool by looking for DotnetTool package type in nuspec
                if (await IsDotnetToolAsync(versionDir, cancellationToken))
                {
                    var command = await GetToolCommandAsync(versionDir, packageId, cancellationToken);
                    tools.Add(new InstalledTool(packageId, version, command));
                }
            }
        }

        // Sort by package ID for consistent output
        tools.Sort((a, b) => string.Compare(a.PackageId, b.PackageId, StringComparison.OrdinalIgnoreCase));

        return tools;
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
    public async Task<bool> RemoveToolAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var packageDirResult = _cacheDirectory.Append(packageId.ToLowerInvariant());
            if (packageDirResult.IsFailure)
            {
                return false;
            }

            var packageDir = packageDirResult.Value;

            var existsResult = await _fileSystem.DirectoryExistsAsync(packageDir, cancellationToken);
            if (existsResult.IsFailure || !existsResult.Value)
            {
                return false;
            }

            var deleteResult = await _fileSystem.DeleteDirectoryAsync(packageDir, recursive: true, cancellationToken);
            return deleteResult.IsSuccess;
        }
        catch
        {
            return false;
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
    public async Task<string?> GetToolExecutablePathAsync(string packageId, string? version = null, CancellationToken cancellationToken = default)
    {
        var packageDirResult = _cacheDirectory.Append(packageId.ToLowerInvariant());
        if (packageDirResult.IsFailure)
        {
            return null;
        }

        var packageDir = packageDirResult.Value;

        var existsResult = await _fileSystem.DirectoryExistsAsync(packageDir, cancellationToken);
        if (existsResult.IsFailure || !existsResult.Value)
        {
            return null;
        }

        // If version not specified, find the latest version
        string? targetVersion = version;
        if (string.IsNullOrEmpty(targetVersion))
        {
            targetVersion = await GetLatestCachedVersionAsync(packageDir, cancellationToken);
            if (targetVersion == null)
            {
                return null;
            }
        }

        var versionDirResult = packageDir.Append(targetVersion);
        if (versionDirResult.IsFailure)
        {
            return null;
        }

        var versionDir = versionDirResult.Value;

        var versionExistsResult = await _fileSystem.DirectoryExistsAsync(versionDir, cancellationToken);
        if (versionExistsResult.IsFailure || !versionExistsResult.Value)
        {
            return null;
        }

        // Verify this is a .NET tool
        if (!await IsDotnetToolAsync(versionDir, cancellationToken))
        {
            return null;
        }

        // Find the executable DLL by looking for .runtimeconfig.json
        var executablePath = await FindExecutableDllAsync(versionDir, cancellationToken);
        return executablePath;
    }

    /// <summary>
    /// Gets the latest version from cached version directories.
    /// </summary>
    private async Task<string?> GetLatestCachedVersionAsync(CanonicalPath packageDir, CancellationToken cancellationToken)
    {
        var versionDirsResult = await _fileSystem.GetDirectoriesAsync(packageDir, "*", recursive: false, cancellationToken);
        if (versionDirsResult.IsFailure || versionDirsResult.Value.Count == 0)
        {
            return null;
        }

        var versionDirs = versionDirsResult.Value;

        // Parse versions and find the latest
        var versions = new List<(string Dir, SemanticVersion? Version)>();
        foreach (var dir in versionDirs)
        {
            var versionString = dir.GetName();
            if (SemanticVersion.TryParse(versionString, out var semVer))
            {
                versions.Add((versionString, semVer));
            }
        }

        if (versions.Count == 0)
        {
            return versionDirs[0].GetName();
        }

        return versions.OrderByDescending(v => v.Version).First().Dir;
    }

    /// <summary>
    /// Finds the executable DLL in the tools directory by looking for .runtimeconfig.json files.
    /// </summary>
    private async Task<string?> FindExecutableDllAsync(CanonicalPath versionDir, CancellationToken cancellationToken)
    {
        var toolsDirResult = versionDir.Append("tools");
        if (toolsDirResult.IsFailure)
        {
            return null;
        }

        var toolsDir = toolsDirResult.Value;

        var toolsDirExistsResult = await _fileSystem.DirectoryExistsAsync(toolsDir, cancellationToken);
        if (toolsDirExistsResult.IsFailure || !toolsDirExistsResult.Value)
        {
            return null;
        }

        // Search for .runtimeconfig.json files which indicate the entry point
        var runtimeConfigFilesResult = await _fileSystem.GetFilesAsync(toolsDir, "*.runtimeconfig.json", recursive: true, cancellationToken);
        if (runtimeConfigFilesResult.IsFailure || runtimeConfigFilesResult.Value.Count == 0)
        {
            return null;
        }

        // Get the DLL name from the runtimeconfig filename
        var runtimeConfigPath = runtimeConfigFilesResult.Value[0];
        var runtimeConfigName = runtimeConfigPath.GetName();
        var dllName = runtimeConfigName.Replace(".runtimeconfig.json", ".dll");

        var parentResult = runtimeConfigPath.GetParent();
        if (parentResult.IsFailure)
        {
            return null;
        }

        var dllPathResult = parentResult.Value.Append(dllName);
        if (dllPathResult.IsFailure)
        {
            return null;
        }

        var dllPath = dllPathResult.Value;

        var dllExistsResult = await _fileSystem.FileExistsAsync(dllPath, cancellationToken);
        if (dllExistsResult.IsFailure || !dllExistsResult.Value)
        {
            return null;
        }

        return dllPath.Value;
    }

    /// <summary>
    /// Checks if the package is a .NET tool by looking for DotnetTool package type in nuspec.
    /// </summary>
    private async Task<bool> IsDotnetToolAsync(CanonicalPath versionDir, CancellationToken cancellationToken)
    {
        try
        {
            var nuspecFilesResult = await _fileSystem.GetFilesAsync(versionDir, "*.nuspec", recursive: false, cancellationToken);
            if (nuspecFilesResult.IsFailure || nuspecFilesResult.Value.Count == 0)
            {
                return false;
            }

            var nuspecPath = nuspecFilesResult.Value[0];
            var nuspecContentResult = await _fileSystem.ReadAllTextAsync(nuspecPath, cancellationToken);
            if (nuspecContentResult.IsFailure)
            {
                return false;
            }

            var nuspec = XDocument.Parse(nuspecContentResult.Value);
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
    private async Task<string> GetToolCommandAsync(CanonicalPath versionDir, string packageId, CancellationToken cancellationToken)
    {
        try
        {
            var nuspecFilesResult = await _fileSystem.GetFilesAsync(versionDir, "*.nuspec", recursive: false, cancellationToken);
            if (nuspecFilesResult.IsSuccess && nuspecFilesResult.Value.Count > 0)
            {
                var nuspecPath = nuspecFilesResult.Value[0];
                var nuspecContentResult = await _fileSystem.ReadAllTextAsync(nuspecPath, cancellationToken);
                if (nuspecContentResult.IsSuccess)
                {
                    var nuspec = XDocument.Parse(nuspecContentResult.Value);
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
        }
        catch
        {
            // Ignore parsing errors
        }

        // Fallback to package ID as the command name
        return packageId;
    }
}
