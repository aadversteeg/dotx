using Core.Domain;

namespace Core.Application;

public interface IToolRunner
{
    Task<string?> GetInstalledVersionAsync(string packageId, CancellationToken cancellationToken = default);
    Task<bool> UpdateToolAsync(string packageId, CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(ToolSpec toolSpec, string[] args, CancellationToken cancellationToken = default);
}
