using Core.Domain;

namespace Core.Application;

/// <summary>
/// Provides operations for managing the cache of globally installed .NET tools.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Lists all globally installed .NET tools.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of installed tools.</returns>
    Task<IReadOnlyList<InstalledTool>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific installed tool (first version found).
    /// </summary>
    /// <param name="packageId">The NuGet package ID of the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The installed tool, or null if not installed.</returns>
    Task<InstalledTool?> GetToolAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cached versions of a specific tool.
    /// </summary>
    /// <param name="packageId">The NuGet package ID of the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of all cached versions of the tool.</returns>
    Task<IReadOnlyList<InstalledTool>> GetToolVersionsAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific tool from the global cache.
    /// </summary>
    /// <param name="packageId">The NuGet package ID of the tool to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the tool was removed; false if it was not installed or removal failed.</returns>
    Task<bool> RemoveToolAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all tools from the global cache.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of tools removed.</returns>
    Task<int> ClearAllToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to the executable DLL for a cached tool.
    /// </summary>
    /// <param name="packageId">The NuGet package ID of the tool.</param>
    /// <param name="version">Optional specific version. If null, returns the latest cached version.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The full path to the executable DLL, or null if not found.</returns>
    Task<string?> GetToolExecutablePathAsync(string packageId, string? version = null, CancellationToken cancellationToken = default);
}
