using Core.Domain;

namespace Core.Application;

/// <summary>
/// Orchestrates the execution of .NET tools with automatic update checking.
/// Prefers running from cache for faster startup and offline support.
/// </summary>
public class ToolExecutor
{
    private readonly INuGetClient _nuGetClient;
    private readonly IToolRunner _toolRunner;
    private readonly ICacheManager _cacheManager;
    private readonly Action<string>? _logAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolExecutor"/> class.
    /// </summary>
    /// <param name="nuGetClient">The NuGet client for checking latest versions.</param>
    /// <param name="toolRunner">The tool runner for executing .NET tools.</param>
    /// <param name="cacheManager">The cache manager for finding cached tools.</param>
    /// <param name="logAction">Optional action for logging messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public ToolExecutor(
        INuGetClient nuGetClient,
        IToolRunner toolRunner,
        ICacheManager cacheManager,
        Action<string>? logAction = null)
    {
        _nuGetClient = nuGetClient ?? throw new ArgumentNullException(nameof(nuGetClient));
        _toolRunner = toolRunner ?? throw new ArgumentNullException(nameof(toolRunner));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _logAction = logAction;
    }

    /// <summary>
    /// Executes a .NET tool, optionally checking for updates in the background.
    /// Prefers running from cache for faster startup and offline support.
    /// Falls back to dotnet tool exec for auto-installation if not cached.
    /// </summary>
    /// <param name="toolSpec">The tool specification containing package ID and optional version.</param>
    /// <param name="toolArgs">The arguments to pass to the tool.</param>
    /// <param name="skipUpdate">If true, skips the background update check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code from the tool execution.</returns>
    public async Task<int> ExecuteAsync(
        ToolSpec toolSpec,
        string[] toolArgs,
        bool skipUpdate = false,
        CancellationToken cancellationToken = default)
    {
        // Start background update check if enabled
        Task? updateTask = null;
        if (!toolSpec.IsPinned && !skipUpdate)
        {
            // Fire and forget - update happens in background
            updateTask = UpdateInBackgroundAsync(toolSpec.PackageId);
        }

        // Try to run from cache first (faster startup, works offline)
        var executablePath = await _cacheManager.GetToolExecutablePathAsync(
            toolSpec.PackageId,
            toolSpec.Version,
            cancellationToken);

        int result;
        if (executablePath != null)
        {
            Log($"Running from cache: {executablePath}");
            result = await _toolRunner.ExecuteFromCacheAsync(executablePath, toolArgs, cancellationToken);
        }
        else
        {
            // Not cached - use dotnet tool exec for auto-installation
            Log($"Tool not cached, using dotnet tool exec for auto-installation...");
            result = await _toolRunner.ExecuteAsync(toolSpec, toolArgs, cancellationToken);
        }

        // Wait for update task to complete if it was started
        // This ensures the latest version is cached for next run
        if (updateTask != null)
        {
            try
            {
                await updateTask;
            }
            catch
            {
                // Ignore update errors - tool execution succeeded
            }
        }

        return result;
    }

    private async Task UpdateInBackgroundAsync(string packageId)
    {
        try
        {
            // Check what's in cache
            var cachedTool = await _cacheManager.GetToolAsync(packageId);
            var cachedVersion = cachedTool?.Version;

            // Check latest version on NuGet
            var latestVersion = await _nuGetClient.GetLatestVersionAsync(packageId);

            if (latestVersion == null)
            {
                return;
            }

            if (cachedVersion == null || IsNewerVersion(latestVersion, cachedVersion))
            {
                Log($"Downloading {packageId}@{latestVersion} in background...");
                var downloadedVersion = await _nuGetClient.DownloadPackageAsync(packageId, latestVersion);
                if (downloadedVersion != null)
                {
                    Log($"Downloaded {packageId}@{downloadedVersion} (will be used on next run)");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Background update check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines whether the latest version is newer than the installed version.
    /// </summary>
    /// <param name="latestVersion">The latest available version string.</param>
    /// <param name="installedVersion">The currently installed version string.</param>
    /// <returns>True if <paramref name="latestVersion"/> is newer than <paramref name="installedVersion"/>; otherwise, false.</returns>
    public static bool IsNewerVersion(string latestVersion, string installedVersion)
    {
        if (SemanticVersion.TryParse(latestVersion, out var latest) &&
            SemanticVersion.TryParse(installedVersion, out var installed))
        {
            return latest! > installed!;
        }

        return string.Compare(latestVersion, installedVersion, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private void Log(string message) => _logAction?.Invoke(message);
}
