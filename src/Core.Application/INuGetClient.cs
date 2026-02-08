namespace Core.Application;

/// <summary>
/// Provides access to NuGet package information.
/// </summary>
public interface INuGetClient
{
    /// <summary>
    /// Gets the latest version of a NuGet package.
    /// </summary>
    /// <param name="packageId">The NuGet package ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The latest version string, or null if the package was not found or an error occurred.</returns>
    Task<string?> GetLatestVersionAsync(string packageId, CancellationToken cancellationToken = default);
}
