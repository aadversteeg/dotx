using Core.Application;
using Core.Domain;
using FluentAssertions;
using Moq;

namespace UnitTests.Application;

public class CacheCommandHandlerTests
{
    private readonly Mock<INuGetClient> _nuGetClientMock;
    private readonly Mock<ICacheManager> _cacheManagerMock;
    private readonly StringWriter _output;
    private readonly StringWriter _error;
    private readonly CacheCommandHandler _sut;

    public CacheCommandHandlerTests()
    {
        _nuGetClientMock = new Mock<INuGetClient>();
        _cacheManagerMock = new Mock<ICacheManager>();
        _output = new StringWriter();
        _error = new StringWriter();
        _sut = new CacheCommandHandler(
            _nuGetClientMock.Object,
            _cacheManagerMock.Object,
            _output,
            _error);
    }

    [Fact(DisplayName = "CCH-001: HandleAsync with no args should print usage and return 1")]
    public async Task CCH001()
    {
        var result = await _sut.HandleAsync([]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Usage:");
    }

    [Fact(DisplayName = "CCH-002: HandleAsync with unknown command should print error and return 1")]
    public async Task CCH002()
    {
        var result = await _sut.HandleAsync(["unknown"]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Unknown cache command: unknown");
    }

    [Fact(DisplayName = "CCH-003: HandleListAsync with no tools should print message")]
    public async Task CCH003()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.HandleListAsync();

        result.Should().Be(0);
        _output.ToString().Should().Contain("No tools installed.");
    }

    [Fact(DisplayName = "CCH-004: HandleListAsync with tools should print table")]
    public async Task CCH004()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InstalledTool("my.tool", "1.0.0", "mytool"),
                new InstalledTool("other.tool", "2.0.0", "othertool")
            ]);

        var result = await _sut.HandleListAsync();

        result.Should().Be(0);
        var output = _output.ToString();
        output.Should().Contain("my.tool");
        output.Should().Contain("1.0.0");
        output.Should().Contain("mytool");
        output.Should().Contain("2 tools installed");
    }

    [Fact(DisplayName = "CCH-005: HandleShowAsync with no package id should return 1")]
    public async Task CCH005()
    {
        var result = await _sut.HandleShowAsync([]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Package ID required");
    }

    [Fact(DisplayName = "CCH-006: HandleShowAsync with nonexistent package should return 1")]
    public async Task CCH006()
    {
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.HandleShowAsync(["nonexistent"]);

        result.Should().Be(1);
        _output.ToString().Should().Contain("not installed");
    }

    [Fact(DisplayName = "CCH-007: HandleShowAsync with existing package should show details")]
    public async Task CCH007()
    {
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InstalledTool("my.tool", "1.0.0", "mytool"),
                new InstalledTool("my.tool", "2.0.0", "mytool")
            ]);

        var result = await _sut.HandleShowAsync(["my.tool"]);

        result.Should().Be(0);
        var output = _output.ToString();
        output.Should().Contain("my.tool");
        output.Should().Contain("1.0.0, 2.0.0");
    }

    [Fact(DisplayName = "CCH-008: HandleAddAsync with no package id should return 1")]
    public async Task CCH008()
    {
        var result = await _sut.HandleAddAsync([]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Package ID required");
    }

    [Fact(DisplayName = "CCH-009: HandleAddAsync with invalid spec should return 1")]
    public async Task CCH009()
    {
        var result = await _sut.HandleAddAsync(["invalid@@@spec"]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Error:");
    }

    [Fact(DisplayName = "CCH-010: HandleAddAsync with valid package should download")]
    public async Task CCH010()
    {
        _nuGetClientMock.Setup(x => x.DownloadPackageAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1.0.0");

        var result = await _sut.HandleAddAsync(["my.tool"]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("Added my.tool (1.0.0)");
    }

    [Fact(DisplayName = "CCH-011: HandleAddAsync with versioned package should download specific version")]
    public async Task CCH011()
    {
        _nuGetClientMock.Setup(x => x.DownloadPackageAsync("my.tool", "2.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0.0");

        var result = await _sut.HandleAddAsync(["my.tool@2.0.0"]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("Added my.tool (2.0.0)");
    }

    [Fact(DisplayName = "CCH-012: HandleAddAsync with download failure should return 1")]
    public async Task CCH012()
    {
        _nuGetClientMock.Setup(x => x.DownloadPackageAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _sut.HandleAddAsync(["my.tool"]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Failed to download");
    }

    [Fact(DisplayName = "CCH-013: HandleUpdateAsync with no tools should print message")]
    public async Task CCH013()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.HandleUpdateAsync([]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("No tools installed");
    }

    [Fact(DisplayName = "CCH-014: HandleUpdateAsync should update outdated tools")]
    public async Task CCH014()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0.0");
        _nuGetClientMock.Setup(x => x.DownloadPackageAsync("my.tool", "2.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0.0");

        var result = await _sut.HandleUpdateAsync([]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("Updated my.tool to 2.0.0");
    }

    [Fact(DisplayName = "CCH-015: HandleUpdateAsync should skip up-to-date tools")]
    public async Task CCH015()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync("1.0.0");

        var result = await _sut.HandleUpdateAsync([]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("is up to date");
        _nuGetClientMock.Verify(x => x.DownloadPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CCH-016: HandleUpdateAsync with specific package should update it")]
    public async Task CCH016()
    {
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0.0");
        _nuGetClientMock.Setup(x => x.DownloadPackageAsync("my.tool", "2.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0.0");

        var result = await _sut.HandleUpdateAsync(["my.tool"]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("Updated my.tool to 2.0.0");
    }

    [Fact(DisplayName = "CCH-017: HandleUpdateAsync with nonexistent package should return 1")]
    public async Task CCH017()
    {
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _sut.HandleUpdateAsync(["nonexistent"]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Could not find package");
    }

    [Fact(DisplayName = "CCH-018: HandleRemoveAsync with no package id should return 1")]
    public async Task CCH018()
    {
        var result = await _sut.HandleRemoveAsync([]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Package ID required");
    }

    [Fact(DisplayName = "CCH-019: HandleRemoveAsync with nonexistent package should return 1")]
    public async Task CCH019()
    {
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.HandleRemoveAsync(["nonexistent"]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("not installed");
    }

    [Fact(DisplayName = "CCH-020: HandleRemoveAsync with existing package should remove it")]
    public async Task CCH020()
    {
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InstalledTool("my.tool", "1.0.0", "mytool"),
                new InstalledTool("my.tool", "2.0.0", "mytool")
            ]);
        _cacheManagerMock.Setup(x => x.RemoveToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.HandleRemoveAsync(["my.tool"]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("Removed my.tool (1.0.0, 2.0.0)");
    }

    [Fact(DisplayName = "CCH-021: HandleRemoveAsync with removal failure should return 1")]
    public async Task CCH021()
    {
        _cacheManagerMock.Setup(x => x.GetToolVersionsAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);
        _cacheManagerMock.Setup(x => x.RemoveToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.HandleRemoveAsync(["my.tool"]);

        result.Should().Be(1);
        _error.ToString().Should().Contain("Failed to remove");
    }

    [Fact(DisplayName = "CCH-022: HandleClearAsync with no tools should print message")]
    public async Task CCH022()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.HandleClearAsync(["-y"]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("No tools installed");
    }

    [Fact(DisplayName = "CCH-023: HandleClearAsync with -y flag should skip confirmation")]
    public async Task CCH023()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);
        _cacheManagerMock.Setup(x => x.RemoveToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.HandleClearAsync(["-y"]);

        result.Should().Be(0);
        _output.ToString().Should().Contain("Removed my.tool");
        _output.ToString().Should().Contain("1 tool removed");
    }

    [Fact(DisplayName = "CCH-024: HandleClearAsync with confirmation declined should cancel")]
    public async Task CCH024()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new InstalledTool("my.tool", "1.0.0", "mytool")]);

        var result = await _sut.HandleClearAsync([], confirmationReader: () => "n");

        result.Should().Be(0);
        _output.ToString().Should().Contain("Cancelled");
        _cacheManagerMock.Verify(x => x.RemoveToolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CCH-025: HandleClearAsync with confirmation accepted should remove all")]
    public async Task CCH025()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InstalledTool("my.tool", "1.0.0", "mytool"),
                new InstalledTool("other.tool", "2.0.0", "othertool")
            ]);
        _cacheManagerMock.Setup(x => x.RemoveToolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.HandleClearAsync([], confirmationReader: () => "y");

        result.Should().Be(0);
        _output.ToString().Should().Contain("2 tools removed");
    }

    [Fact(DisplayName = "CCH-026: HandleClearAsync should group multiple versions of same package")]
    public async Task CCH026()
    {
        _cacheManagerMock.Setup(x => x.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InstalledTool("my.tool", "1.0.0", "mytool"),
                new InstalledTool("my.tool", "2.0.0", "mytool"),
                new InstalledTool("other.tool", "1.0.0", "othertool")
            ]);
        _cacheManagerMock.Setup(x => x.RemoveToolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.HandleClearAsync(["-y"]);

        result.Should().Be(0);
        var output = _output.ToString();
        output.Should().Contain("Removed my.tool (1.0.0, 2.0.0)");
        output.Should().Contain("Removed other.tool (1.0.0)");
        output.Should().Contain("2 tools removed");
        // Should only call RemoveToolAsync twice (once per unique package)
        _cacheManagerMock.Verify(x => x.RemoveToolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
