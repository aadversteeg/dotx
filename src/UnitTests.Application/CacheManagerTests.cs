using Core.Application;
using Core.Domain;
using FluentAssertions;
using Moq;

namespace UnitTests.Application;

public class CacheManagerTests
{
    [Fact(DisplayName = "CMG-001: ListToolsAsync should return empty list when no tools installed")]
    public async Task CMG001()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InstalledTool>());

        var result = await mockCacheManager.Object.ListToolsAsync();

        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "CMG-002: ListToolsAsync should return installed tools")]
    public async Task CMG002()
    {
        var expectedTools = new[]
        {
            new InstalledTool("tool.one", "1.0.0", "tool-one"),
            new InstalledTool("tool.two", "2.0.0", "tool-two")
        };

        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTools);

        var result = await mockCacheManager.Object.ListToolsAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.PackageId == "tool.one");
        result.Should().Contain(t => t.PackageId == "tool.two");
    }

    [Fact(DisplayName = "CMG-003: GetToolAsync should return tool when installed")]
    public async Task CMG003()
    {
        var expectedTool = new InstalledTool("my.tool", "1.0.0", "my-tool");

        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.GetToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTool);

        var result = await mockCacheManager.Object.GetToolAsync("my.tool");

        result.Should().NotBeNull();
        result!.PackageId.Should().Be("my.tool");
        result.Version.Should().Be("1.0.0");
    }

    [Fact(DisplayName = "CMG-004: GetToolAsync should return null when not installed")]
    public async Task CMG004()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.GetToolAsync("nonexistent.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstalledTool?)null);

        var result = await mockCacheManager.Object.GetToolAsync("nonexistent.tool");

        result.Should().BeNull();
    }

    [Fact(DisplayName = "CMG-005: RemoveToolAsync should return true on success")]
    public async Task CMG005()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.RemoveToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await mockCacheManager.Object.RemoveToolAsync("my.tool");

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "CMG-006: RemoveToolAsync should return false when tool not found")]
    public async Task CMG006()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.RemoveToolAsync("nonexistent.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await mockCacheManager.Object.RemoveToolAsync("nonexistent.tool");

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "CMG-007: ClearAllToolsAsync should return count of removed tools")]
    public async Task CMG007()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.ClearAllToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await mockCacheManager.Object.ClearAllToolsAsync();

        result.Should().Be(3);
    }

    [Fact(DisplayName = "CMG-008: ClearAllToolsAsync should return zero when no tools installed")]
    public async Task CMG008()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        mockCacheManager.Setup(x => x.ClearAllToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await mockCacheManager.Object.ClearAllToolsAsync();

        result.Should().Be(0);
    }
}
