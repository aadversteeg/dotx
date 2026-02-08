namespace Core.Application;

public interface INuGetClient
{
    Task<string?> GetLatestVersionAsync(string packageId, CancellationToken cancellationToken = default);
}
