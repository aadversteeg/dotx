namespace Core.Domain;

/// <summary>
/// Represents a .NET tool specification with package ID and optional version.
/// </summary>
public sealed class ToolSpec
{
    /// <summary>
    /// Gets the NuGet package ID of the tool.
    /// </summary>
    public string PackageId { get; }

    /// <summary>
    /// Gets the pinned version of the tool, or null if not pinned.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Gets a value indicating whether this tool spec has a pinned version.
    /// </summary>
    public bool IsPinned => Version != null;

    private ToolSpec(string packageId, string? version)
    {
        PackageId = packageId;
        Version = version;
    }

    /// <summary>
    /// Parses a tool specification string into a <see cref="ToolSpec"/> instance.
    /// </summary>
    /// <param name="toolSpec">The tool specification string in the format "packageId" or "packageId@version".</param>
    /// <returns>A <see cref="ToolSpec"/> instance representing the parsed specification.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toolSpec"/> is null or whitespace.</exception>
    public static ToolSpec Parse(string toolSpec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolSpec);

        var atIndex = toolSpec.LastIndexOf('@');
        if (atIndex > 0)
        {
            var packageId = toolSpec[..atIndex];
            var version = toolSpec[(atIndex + 1)..];
            return new ToolSpec(packageId, version);
        }

        return new ToolSpec(toolSpec, null);
    }

    /// <inheritdoc/>
    public override string ToString() => IsPinned ? $"{PackageId}@{Version}" : PackageId;
}
