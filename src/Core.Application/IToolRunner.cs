using Core.Domain;

namespace Core.Application;

/// <summary>
/// Provides operations for executing .NET tools.
/// </summary>
public interface IToolRunner
{
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
