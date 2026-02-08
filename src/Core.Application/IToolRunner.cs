using Core.Domain;

namespace Core.Application;

/// <summary>
/// Provides operations for managing and executing .NET tools.
/// </summary>
public interface IToolRunner
{
    /// <summary>
    /// Gets the installed version of a globally installed .NET tool.
    /// </summary>
    /// <param name="packageId">The NuGet package ID of the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The installed version string, or null if not installed or an error occurred.</returns>
    Task<string?> GetInstalledVersionAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a globally installed .NET tool to the latest version.
    /// </summary>
    /// <param name="packageId">The NuGet package ID of the tool to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the update succeeded; otherwise, false.</returns>
    Task<bool> UpdateToolAsync(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a .NET tool with the specified arguments.
    /// </summary>
    /// <param name="toolSpec">The tool specification containing package ID and optional version.</param>
    /// <param name="args">The arguments to pass to the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code from the tool execution.</returns>
    Task<int> ExecuteAsync(ToolSpec toolSpec, string[] args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a .NET tool directly from its cached DLL path.
    /// This allows running tools without network access.
    /// </summary>
    /// <param name="dllPath">The full path to the tool's executable DLL.</param>
    /// <param name="args">The arguments to pass to the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exit code from the tool execution.</returns>
    Task<int> ExecuteFromCacheAsync(string dllPath, string[] args, CancellationToken cancellationToken = default);
}
