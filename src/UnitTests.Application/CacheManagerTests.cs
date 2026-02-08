using Ave.Extensions.FileSystem;
using Ave.Extensions.FileSystem.InMemory;
using Core.Infrastructure.ConsoleApp.Services;
using FluentAssertions;

namespace UnitTests.Application;

public class CacheManagerTests
{
    private static readonly CanonicalPath CacheRoot = CanonicalPath.Create("/cache").Value;

    [Fact(DisplayName = "CMG-001: ListToolsAsync should return empty list when cache directory does not exist")]
    public async Task CMG001()
    {
        var fs = new InMemoryFileSystem();
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ListToolsAsync();

        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "CMG-002: ListToolsAsync should return empty list when no tools installed")]
    public async Task CMG002()
    {
        var fs = new InMemoryFileSystem();
        await fs.CreateDirectoryAsync(CacheRoot);
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ListToolsAsync();

        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "CMG-003: ListToolsAsync should return installed dotnet tools")]
    public async Task CMG003()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "my.tool", "1.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ListToolsAsync();

        result.Should().HaveCount(1);
        result[0].PackageId.Should().Be("my.tool");
        result[0].Version.Should().Be("1.0.0");
        result[0].Commands.Should().Be("my-tool");
    }

    [Fact(DisplayName = "CMG-004: ListToolsAsync should return multiple tools sorted by package id")]
    public async Task CMG004()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "zebra.tool", "1.0.0", "zebra");
        await SetupToolAsync(fs, "alpha.tool", "2.0.0", "alpha");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ListToolsAsync();

        result.Should().HaveCount(2);
        result[0].PackageId.Should().Be("alpha.tool");
        result[1].PackageId.Should().Be("zebra.tool");
    }

    [Fact(DisplayName = "CMG-005: ListToolsAsync should skip packages that are not dotnet tools")]
    public async Task CMG005()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "real.tool", "1.0.0", "real");
        await SetupNonToolPackageAsync(fs, "library.package", "1.0.0");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ListToolsAsync();

        result.Should().HaveCount(1);
        result[0].PackageId.Should().Be("real.tool");
    }

    [Fact(DisplayName = "CMG-006: GetToolAsync should return tool when installed")]
    public async Task CMG006()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "my.tool", "1.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolAsync("my.tool");

        result.Should().NotBeNull();
        result!.PackageId.Should().Be("my.tool");
        result.Version.Should().Be("1.0.0");
    }

    [Fact(DisplayName = "CMG-007: GetToolAsync should return null when not installed")]
    public async Task CMG007()
    {
        var fs = new InMemoryFileSystem();
        await fs.CreateDirectoryAsync(CacheRoot);
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolAsync("nonexistent.tool");

        result.Should().BeNull();
    }

    [Fact(DisplayName = "CMG-008: GetToolAsync should be case insensitive")]
    public async Task CMG008()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "my.tool", "1.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolAsync("MY.TOOL");

        result.Should().NotBeNull();
        result!.PackageId.Should().Be("my.tool");
    }

    [Fact(DisplayName = "CMG-009: RemoveToolAsync should return true and delete directory on success")]
    public async Task CMG009()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "my.tool", "1.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.RemoveToolAsync("my.tool");

        result.Should().BeTrue();
        var toolDir = CacheRoot.Append("my.tool").Value;
        var exists = await fs.DirectoryExistsAsync(toolDir);
        exists.Value.Should().BeFalse();
    }

    [Fact(DisplayName = "CMG-010: RemoveToolAsync should return false when tool not found")]
    public async Task CMG010()
    {
        var fs = new InMemoryFileSystem();
        await fs.CreateDirectoryAsync(CacheRoot);
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.RemoveToolAsync("nonexistent.tool");

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "CMG-011: ClearAllToolsAsync should remove all tools and return count")]
    public async Task CMG011()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "tool.one", "1.0.0", "one");
        await SetupToolAsync(fs, "tool.two", "2.0.0", "two");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ClearAllToolsAsync();

        result.Should().Be(2);
        var remaining = await cacheManager.ListToolsAsync();
        remaining.Should().BeEmpty();
    }

    [Fact(DisplayName = "CMG-012: ClearAllToolsAsync should return zero when no tools installed")]
    public async Task CMG012()
    {
        var fs = new InMemoryFileSystem();
        await fs.CreateDirectoryAsync(CacheRoot);
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ClearAllToolsAsync();

        result.Should().Be(0);
    }

    [Fact(DisplayName = "CMG-013: GetToolVersionsAsync should return all versions of a tool")]
    public async Task CMG013()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolAsync(fs, "my.tool", "1.0.0", "my-tool");
        await SetupToolAsync(fs, "my.tool", "2.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolVersionsAsync("my.tool");

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Version == "1.0.0");
        result.Should().Contain(t => t.Version == "2.0.0");
    }

    [Fact(DisplayName = "CMG-014: GetToolExecutablePathAsync should return path to DLL")]
    public async Task CMG014()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolWithExecutableAsync(fs, "my.tool", "1.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolExecutablePathAsync("my.tool", "1.0.0");

        result.Should().NotBeNull();
        result.Should().EndWith("my-tool.dll");
    }

    [Fact(DisplayName = "CMG-015: GetToolExecutablePathAsync should return null when tool not found")]
    public async Task CMG015()
    {
        var fs = new InMemoryFileSystem();
        await fs.CreateDirectoryAsync(CacheRoot);
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolExecutablePathAsync("nonexistent.tool");

        result.Should().BeNull();
    }

    [Fact(DisplayName = "CMG-016: GetToolExecutablePathAsync should find latest version when not specified")]
    public async Task CMG016()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolWithExecutableAsync(fs, "my.tool", "1.0.0", "my-tool");
        await SetupToolWithExecutableAsync(fs, "my.tool", "2.0.0", "my-tool");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.GetToolExecutablePathAsync("my.tool");

        result.Should().NotBeNull();
        result.Should().Contain("2.0.0");
    }

    [Fact(DisplayName = "CMG-017: ListToolsAsync should use package id as command when toolCommandName not in nuspec")]
    public async Task CMG017()
    {
        var fs = new InMemoryFileSystem();
        await SetupToolWithoutCommandNameAsync(fs, "my.tool", "1.0.0");
        var cacheManager = new DotnetCacheManager(fs, CacheRoot);

        var result = await cacheManager.ListToolsAsync();

        result.Should().HaveCount(1);
        result[0].Commands.Should().Be("my.tool");
    }

    private static async Task SetupToolAsync(InMemoryFileSystem fs, string packageId, string version, string commandName)
    {
        var packageDir = CacheRoot.Append(packageId.ToLowerInvariant()).Value;
        var versionDir = packageDir.Append(version).Value;
        await fs.CreateDirectoryAsync(versionDir);

        var nuspecPath = versionDir.Append($"{packageId}.nuspec").Value;
        var nuspecContent = CreateNuspecContent(packageId, version, commandName, isDotnetTool: true);
        await fs.WriteAllTextAsync(nuspecPath, nuspecContent);
    }

    private static async Task SetupToolWithExecutableAsync(InMemoryFileSystem fs, string packageId, string version, string commandName)
    {
        await SetupToolAsync(fs, packageId, version, commandName);

        var packageDir = CacheRoot.Append(packageId.ToLowerInvariant()).Value;
        var versionDir = packageDir.Append(version).Value;
        var toolsDir = versionDir.Append("tools").Value;
        var netDir = toolsDir.Append("net8.0").Value;
        var anyDir = netDir.Append("any").Value;
        await fs.CreateDirectoryAsync(anyDir);

        var runtimeConfigPath = anyDir.Append($"{commandName}.runtimeconfig.json").Value;
        await fs.WriteAllTextAsync(runtimeConfigPath, "{}");

        var dllPath = anyDir.Append($"{commandName}.dll").Value;
        await fs.WriteAllTextAsync(dllPath, "");
    }

    private static async Task SetupToolWithoutCommandNameAsync(InMemoryFileSystem fs, string packageId, string version)
    {
        var packageDir = CacheRoot.Append(packageId.ToLowerInvariant()).Value;
        var versionDir = packageDir.Append(version).Value;
        await fs.CreateDirectoryAsync(versionDir);

        var nuspecPath = versionDir.Append($"{packageId}.nuspec").Value;
        var nuspecContent = CreateNuspecContent(packageId, version, toolCommandName: null, isDotnetTool: true);
        await fs.WriteAllTextAsync(nuspecPath, nuspecContent);
    }

    private static async Task SetupNonToolPackageAsync(InMemoryFileSystem fs, string packageId, string version)
    {
        var packageDir = CacheRoot.Append(packageId.ToLowerInvariant()).Value;
        var versionDir = packageDir.Append(version).Value;
        await fs.CreateDirectoryAsync(versionDir);

        var nuspecPath = versionDir.Append($"{packageId}.nuspec").Value;
        var nuspecContent = CreateNuspecContent(packageId, version, toolCommandName: null, isDotnetTool: false);
        await fs.WriteAllTextAsync(nuspecPath, nuspecContent);
    }

    private static string CreateNuspecContent(string packageId, string version, string? toolCommandName, bool isDotnetTool)
    {
        var toolCommandElement = toolCommandName != null
            ? $"<toolCommandName>{toolCommandName}</toolCommandName>"
            : "";

        var packageTypesElement = isDotnetTool
            ? @"<packageTypes>
        <packageType name=""DotnetTool"" />
      </packageTypes>"
            : "";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"">
  <metadata>
    <id>{packageId}</id>
    <version>{version}</version>
    {toolCommandElement}
    {packageTypesElement}
  </metadata>
</package>";
    }
}
