using Core.Domain;

namespace Core.Application;

public class ToolExecutor
{
    private readonly INuGetClient _nuGetClient;
    private readonly IToolRunner _toolRunner;
    private readonly Action<string>? _logAction;

    public ToolExecutor(INuGetClient nuGetClient, IToolRunner toolRunner, Action<string>? logAction = null)
    {
        _nuGetClient = nuGetClient ?? throw new ArgumentNullException(nameof(nuGetClient));
        _toolRunner = toolRunner ?? throw new ArgumentNullException(nameof(toolRunner));
        _logAction = logAction;
    }

    public async Task<int> ExecuteAsync(
        ToolSpec toolSpec,
        string[] toolArgs,
        bool skipUpdate = false,
        CancellationToken cancellationToken = default)
    {
        if (!toolSpec.IsPinned && !skipUpdate)
        {
            // Fire and forget - update happens in background
            // Tool will use updated version on next run
            _ = UpdateInBackgroundAsync(toolSpec.PackageId);
        }

        return await _toolRunner.ExecuteAsync(toolSpec, toolArgs, cancellationToken);
    }

    private async Task UpdateInBackgroundAsync(string packageId)
    {
        try
        {
            var installedVersionTask = _toolRunner.GetInstalledVersionAsync(packageId);
            var latestVersionTask = _nuGetClient.GetLatestVersionAsync(packageId);

            await Task.WhenAll(installedVersionTask, latestVersionTask);

            var installedVersion = await installedVersionTask;
            var latestVersion = await latestVersionTask;

            if (latestVersion == null || installedVersion == null)
            {
                return;
            }

            if (IsNewerVersion(latestVersion, installedVersion))
            {
                Log($"Updating {packageId} from {installedVersion} to {latestVersion} in background...");
                var updated = await _toolRunner.UpdateToolAsync(packageId);
                if (updated)
                {
                    Log($"Updated {packageId} to {latestVersion} (will be used on next run)");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Background update check failed: {ex.Message}");
        }
    }

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
