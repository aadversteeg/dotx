using Core.Domain;

namespace Core.Application;

/// <summary>
/// Handles cache-related CLI commands.
/// </summary>
public class CacheCommandHandler
{
    private readonly INuGetClient _nuGetClient;
    private readonly ICacheManager _cacheManager;
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheCommandHandler"/> class.
    /// </summary>
    /// <param name="nuGetClient">The NuGet client for downloading packages.</param>
    /// <param name="cacheManager">The cache manager for managing cached tools.</param>
    /// <param name="output">The output writer for standard output.</param>
    /// <param name="error">The output writer for error output.</param>
    public CacheCommandHandler(INuGetClient nuGetClient, ICacheManager cacheManager, TextWriter output, TextWriter error)
    {
        _nuGetClient = nuGetClient;
        _cacheManager = cacheManager;
        _output = output;
        _error = error;
    }

    /// <summary>
    /// Handles cache subcommands (list, show, add, update, remove, clear).
    /// </summary>
    /// <param name="args">The arguments after 'cache'.</param>
    /// <returns>Exit code.</returns>
    public async Task<int> HandleAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        return args[0] switch
        {
            "list" => await HandleListAsync(),
            "show" => await HandleShowAsync(args.Skip(1).ToArray()),
            "add" => await HandleAddAsync(args.Skip(1).ToArray()),
            "update" => await HandleUpdateAsync(args.Skip(1).ToArray()),
            "remove" => await HandleRemoveAsync(args.Skip(1).ToArray()),
            "clear" => await HandleClearAsync(args.Skip(1).ToArray()),
            _ => PrintUsageWithError($"Unknown cache command: {args[0]}")
        };
    }

    /// <summary>
    /// Handles 'dotx cache list' command.
    /// </summary>
    public async Task<int> HandleListAsync()
    {
        var tools = await _cacheManager.ListToolsAsync();

        if (tools.Count == 0)
        {
            await _output.WriteLineAsync("No tools installed.");
            return 0;
        }

        await _output.WriteLineAsync($"{"Package Id",-40} {"Version",-15} Commands");
        await _output.WriteLineAsync(new string('-', 70));

        foreach (var tool in tools)
        {
            await _output.WriteLineAsync($"{tool.PackageId,-40} {tool.Version,-15} {tool.Commands}");
        }

        await _output.WriteLineAsync();
        await _output.WriteLineAsync($"{tools.Count} tool{(tools.Count == 1 ? "" : "s")} installed");

        return 0;
    }

    /// <summary>
    /// Handles 'dotx cache show <package-id>' command.
    /// </summary>
    public async Task<int> HandleShowAsync(string[] args)
    {
        if (args.Length == 0)
        {
            await _error.WriteLineAsync("Error: Package ID required.");
            await _error.WriteLineAsync("Usage: dotx cache show <package-id>");
            return 1;
        }

        var packageId = args[0];
        var versions = await _cacheManager.GetToolVersionsAsync(packageId);

        if (versions.Count == 0)
        {
            await _output.WriteLineAsync($"Tool '{packageId}' is not installed.");
            return 1;
        }

        await _output.WriteLineAsync($"Package Id: {versions[0].PackageId}");
        await _output.WriteLineAsync($"Commands:   {versions[0].Commands}");
        await _output.WriteLineAsync($"Versions:   {string.Join(", ", versions.Select(v => v.Version))}");

        return 0;
    }

    /// <summary>
    /// Handles 'dotx cache add <package-id>[@version]' command.
    /// </summary>
    public async Task<int> HandleAddAsync(string[] args)
    {
        if (args.Length == 0)
        {
            await _error.WriteLineAsync("Error: Package ID required.");
            await _error.WriteLineAsync("Usage: dotx cache add <package-id>[@version]");
            return 1;
        }

        var toolSpecString = args[0];
        ToolSpec toolSpec;
        try
        {
            toolSpec = ToolSpec.Parse(toolSpecString);
        }
        catch (ArgumentException ex)
        {
            await _error.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }

        await _output.WriteLineAsync($"Downloading {toolSpec}...");
        var downloadedVersion = await _nuGetClient.DownloadPackageAsync(toolSpec.PackageId, toolSpec.Version);

        if (downloadedVersion == null)
        {
            await _error.WriteLineAsync($"Error: Failed to download '{toolSpec}'.");
            return 1;
        }

        await _output.WriteLineAsync($"Added {toolSpec.PackageId} ({downloadedVersion})");
        return 0;
    }

    /// <summary>
    /// Handles 'dotx cache update [package-id]' command.
    /// </summary>
    public async Task<int> HandleUpdateAsync(string[] args)
    {
        if (args.Length == 0)
        {
            // Update all cached tools
            var tools = await _cacheManager.ListToolsAsync();

            if (tools.Count == 0)
            {
                await _output.WriteLineAsync("No tools installed.");
                return 0;
            }

            // Get unique package IDs
            var packageIds = tools.Select(t => t.PackageId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var updatedCount = 0;

            foreach (var packageId in packageIds)
            {
                var latestVersion = await _nuGetClient.GetLatestVersionAsync(packageId);
                if (latestVersion == null)
                {
                    await _error.WriteLineAsync($"Warning: Could not check latest version for '{packageId}'");
                    continue;
                }

                var cachedVersions = await _cacheManager.GetToolVersionsAsync(packageId);
                var highestCached = cachedVersions
                    .Select(t => t.Version)
                    .Select(v => (Version: v, Parsed: SemanticVersion.TryParse(v, out var sv) ? sv : null))
                    .OrderByDescending(x => x.Parsed)
                    .Select(x => x.Version)
                    .FirstOrDefault();

                if (highestCached != null && !ToolExecutor.IsNewerVersion(latestVersion, highestCached))
                {
                    await _output.WriteLineAsync($"{packageId} ({highestCached}) is up to date");
                    continue;
                }

                await _output.WriteLineAsync($"Downloading {packageId}@{latestVersion}...");
                var downloadedVersion = await _nuGetClient.DownloadPackageAsync(packageId, latestVersion);

                if (downloadedVersion != null)
                {
                    await _output.WriteLineAsync($"Updated {packageId} to {downloadedVersion}");
                    updatedCount++;
                }
                else
                {
                    await _error.WriteLineAsync($"Warning: Failed to download '{packageId}@{latestVersion}'");
                }
            }

            await _output.WriteLineAsync();
            await _output.WriteLineAsync($"{updatedCount} tool{(updatedCount == 1 ? "" : "s")} updated");
            return 0;
        }
        else
        {
            // Update specific tool
            var packageId = args[0];
            var latestVersion = await _nuGetClient.GetLatestVersionAsync(packageId);

            if (latestVersion == null)
            {
                await _error.WriteLineAsync($"Error: Could not find package '{packageId}' on NuGet.");
                return 1;
            }

            await _output.WriteLineAsync($"Downloading {packageId}@{latestVersion}...");
            var downloadedVersion = await _nuGetClient.DownloadPackageAsync(packageId, latestVersion);

            if (downloadedVersion == null)
            {
                await _error.WriteLineAsync($"Error: Failed to download '{packageId}@{latestVersion}'.");
                return 1;
            }

            await _output.WriteLineAsync($"Updated {packageId} to {downloadedVersion}");
            return 0;
        }
    }

    /// <summary>
    /// Handles 'dotx cache remove <package-id>' command.
    /// </summary>
    public async Task<int> HandleRemoveAsync(string[] args)
    {
        if (args.Length == 0)
        {
            await _error.WriteLineAsync("Error: Package ID required.");
            await _error.WriteLineAsync("Usage: dotx cache remove <package-id>");
            return 1;
        }

        var packageId = args[0];
        var tools = await _cacheManager.GetToolVersionsAsync(packageId);

        if (tools.Count == 0)
        {
            await _error.WriteLineAsync($"Error: Tool '{packageId}' is not installed.");
            return 1;
        }

        var success = await _cacheManager.RemoveToolAsync(packageId);

        if (success)
        {
            var versions = string.Join(", ", tools.Select(t => t.Version));
            await _output.WriteLineAsync($"Removed {packageId} ({versions})");
            return 0;
        }

        await _error.WriteLineAsync($"Error: Failed to remove '{packageId}'.");
        return 1;
    }

    /// <summary>
    /// Handles 'dotx cache clear [-y]' command.
    /// </summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="confirmationReader">Optional function to read confirmation input. If null, uses Console.ReadLine.</param>
    public async Task<int> HandleClearAsync(string[] args, Func<string?>? confirmationReader = null)
    {
        var skipConfirmation = args.Contains("-y") || args.Contains("--yes");

        var tools = await _cacheManager.ListToolsAsync();

        if (tools.Count == 0)
        {
            await _output.WriteLineAsync("No tools installed.");
            return 0;
        }

        // Group by package ID to get unique packages with all their versions
        var packageGroups = tools
            .GroupBy(t => t.PackageId, StringComparer.OrdinalIgnoreCase)
            .Select(g => (PackageId: g.Key, Versions: g.Select(t => t.Version).ToList()))
            .ToList();

        if (!skipConfirmation)
        {
            await _output.WriteLineAsync($"This will remove {packageGroups.Count} tool{(packageGroups.Count == 1 ? "" : "s")}:");
            foreach (var pkg in packageGroups)
            {
                var versions = string.Join(", ", pkg.Versions);
                await _output.WriteLineAsync($"  - {pkg.PackageId} ({versions})");
            }
            await _output.WriteLineAsync();
            await _output.WriteAsync("Continue? [y/N] ");

            var reader = confirmationReader ?? Console.ReadLine;
            var response = reader();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                await _output.WriteLineAsync("Cancelled.");
                return 0;
            }
        }

        var removedCount = 0;
        foreach (var pkg in packageGroups)
        {
            if (await _cacheManager.RemoveToolAsync(pkg.PackageId))
            {
                var versions = string.Join(", ", pkg.Versions);
                await _output.WriteLineAsync($"Removed {pkg.PackageId} ({versions})");
                removedCount++;
            }
            else
            {
                await _error.WriteLineAsync($"Failed to remove {pkg.PackageId}");
            }
        }

        await _output.WriteLineAsync();
        await _output.WriteLineAsync($"{removedCount} tool{(removedCount == 1 ? "" : "s")} removed");

        return 0;
    }

    /// <summary>
    /// Prints usage with an error message.
    /// </summary>
    private int PrintUsageWithError(string error)
    {
        _error.WriteLine($"Error: {error}");
        PrintUsage();
        return 1;
    }

    /// <summary>
    /// Prints the cache command usage information.
    /// </summary>
    private void PrintUsage()
    {
        _error.WriteLine();
        _error.WriteLine("Usage: dotx cache <command> [options]");
        _error.WriteLine();
        _error.WriteLine("Commands:");
        _error.WriteLine("  list              List all installed tools");
        _error.WriteLine("  show <id>         Show details for a specific tool");
        _error.WriteLine("  add <id>[@ver]    Download a tool to cache without running it");
        _error.WriteLine("  update [<id>]     Update a tool (or all) to latest version");
        _error.WriteLine("  remove <id>       Remove a specific tool");
        _error.WriteLine("  clear [-y]        Remove all tools (use -y to skip confirmation)");
    }
}
