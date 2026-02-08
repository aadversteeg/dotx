namespace Core.Application;

/// <summary>
/// Provides access to NuGet package information and download capabilities.
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

    /// <summary>
    /// Downloads a NuGet package to the local cache.
    /// </summary>
    /// <param name="packageId">The NuGet package ID.</param>
    /// <param name="version">The version to download. If null, downloads the latest version.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The downloaded version string, or null if the download failed.</returns>
    Task<string?> DownloadPackageAsync(string packageId, string? version = null, CancellationToken cancellationToken = default);
}
