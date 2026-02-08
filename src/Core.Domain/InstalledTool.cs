namespace Core.Domain;

/// <summary>
/// Represents a globally installed .NET tool.
/// </summary>
/// <param name="PackageId">The NuGet package ID of the tool.</param>
/// <param name="Version">The installed version of the tool.</param>
/// <param name="Commands">The command(s) provided by the tool.</param>
public sealed record InstalledTool(string PackageId, string Version, string Commands);
