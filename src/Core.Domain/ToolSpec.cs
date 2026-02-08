namespace Core.Domain;

public sealed class ToolSpec
{
    public string PackageId { get; }
    public string? Version { get; }
    public bool IsPinned => Version != null;

    private ToolSpec(string packageId, string? version)
    {
        PackageId = packageId;
        Version = version;
    }

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

    public override string ToString() => IsPinned ? $"{PackageId}@{Version}" : PackageId;
}
