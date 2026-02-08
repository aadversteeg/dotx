using Core.Application;
using Core.Domain;
using FluentAssertions;
using Moq;

namespace UnitTests.Application;

public class ToolExecutorTests
{
    private readonly Mock<INuGetClient> _nuGetClientMock;
    private readonly Mock<IToolRunner> _toolRunnerMock;
    private readonly Mock<ICacheManager> _cacheManagerMock;
    private readonly List<string> _logMessages;
    private readonly ToolExecutor _sut;

    public ToolExecutorTests()
    {
        _nuGetClientMock = new Mock<INuGetClient>();
        _toolRunnerMock = new Mock<IToolRunner>();
        _cacheManagerMock = new Mock<ICacheManager>();
        _logMessages = [];
        _sut = new ToolExecutor(
            _nuGetClientMock.Object,
            _toolRunnerMock.Object,
            _cacheManagerMock.Object,
            msg => _logMessages.Add(msg));
    }

    [Fact(DisplayName = "TEX-001: ExecuteAsync with pinned version should skip update check")]
    public async Task TEX001()
    {
        var toolSpec = ToolSpec.Parse("my.tool@1.0.0");
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.ExecuteAsync(toolSpec, []);

        result.Should().Be(0);
        _nuGetClientMock.Verify(x => x.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "TEX-002: ExecuteAsync with skipUpdate should skip update check")]
    public async Task TEX002()
    {
        var toolSpec = ToolSpec.Parse("my.tool");
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.ExecuteAsync(toolSpec, [], skipUpdate: true);

        result.Should().Be(0);
        _nuGetClientMock.Verify(x => x.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "TEX-003: ExecuteAsync should start background update when newer version available")]
    public async Task TEX003()
    {
        var toolSpec = ToolSpec.Parse("my.tool");
        var updateStarted = new TaskCompletionSource<bool>();

        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _cacheManagerMock.Setup(x => x.GetToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstalledTool("my.tool", "1.0.0", "my-tool"));
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0.0");
        _toolRunnerMock.Setup(x => x.UpdateToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .Callback(() => updateStarted.TrySetResult(true))
            .ReturnsAsync(true);
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ExecuteAsync(toolSpec, []);

        // Wait briefly for background task to start
        var started = await Task.WhenAny(updateStarted.Task, Task.Delay(1000)) == updateStarted.Task;
        started.Should().BeTrue("background update should have started");
    }

    [Fact(DisplayName = "TEX-004: ExecuteAsync should not update when already at latest")]
    public async Task TEX004()
    {
        var toolSpec = ToolSpec.Parse("my.tool");
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _cacheManagerMock.Setup(x => x.GetToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstalledTool("my.tool", "1.0.0", "my-tool"));
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync("1.0.0");
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ExecuteAsync(toolSpec, []);

        // Wait briefly for background task
        await Task.Delay(100);

        _toolRunnerMock.Verify(x => x.UpdateToolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "TEX-005: ExecuteAsync should pass tool args to runner")]
    public async Task TEX005()
    {
        var toolSpec = ToolSpec.Parse("my.tool@1.0.0");
        var toolArgs = new[] { "--arg1", "value1" };
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", toolArgs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _sut.ExecuteAsync(toolSpec, toolArgs);

        result.Should().Be(42);
        _toolRunnerMock.Verify(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", toolArgs, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TEX-006: ExecuteAsync should not block on update check failure")]
    public async Task TEX006()
    {
        var toolSpec = ToolSpec.Parse("my.tool");
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _cacheManagerMock.Setup(x => x.GetToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstalledTool("my.tool", "1.0.0", "my-tool"));
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.ExecuteAsync(toolSpec, []);

        result.Should().Be(0);
    }

    [Fact(DisplayName = "TEX-007: ExecuteAsync should run from cache for faster startup")]
    public async Task TEX007()
    {
        var toolSpec = ToolSpec.Parse("my.tool");
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/path/to/tool.dll");
        _toolRunnerMock.Setup(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ExecuteAsync(toolSpec, [], skipUpdate: true);

        _toolRunnerMock.Verify(x => x.ExecuteFromCacheAsync("/path/to/tool.dll", It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _toolRunnerMock.Verify(x => x.ExecuteAsync(It.IsAny<ToolSpec>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "TEX-008: IsNewerVersion with newer version should return true")]
    public void TEX008()
    {
        ToolExecutor.IsNewerVersion("1.1.0", "1.0.0").Should().BeTrue();
    }

    [Fact(DisplayName = "TEX-009: IsNewerVersion with same version should return false")]
    public void TEX009()
    {
        ToolExecutor.IsNewerVersion("1.0.0", "1.0.0").Should().BeFalse();
    }

    [Fact(DisplayName = "TEX-010: IsNewerVersion with older version should return false")]
    public void TEX010()
    {
        ToolExecutor.IsNewerVersion("1.0.0", "1.1.0").Should().BeFalse();
    }

    [Fact(DisplayName = "TEX-011: ExecuteAsync should fallback to dotnet tool exec when not cached")]
    public async Task TEX011()
    {
        var toolSpec = ToolSpec.Parse("my.tool@1.0.0");
        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _toolRunnerMock.Setup(x => x.ExecuteAsync(toolSpec, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.ExecuteAsync(toolSpec, []);

        result.Should().Be(0);
        _toolRunnerMock.Verify(x => x.ExecuteAsync(toolSpec, It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _toolRunnerMock.Verify(x => x.ExecuteFromCacheAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "TEX-012: ExecuteAsync should download new version when not in cache")]
    public async Task TEX012()
    {
        var toolSpec = ToolSpec.Parse("my.tool");
        var downloadStarted = new TaskCompletionSource<bool>();

        _cacheManagerMock.Setup(x => x.GetToolExecutablePathAsync("my.tool", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _cacheManagerMock.Setup(x => x.GetToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstalledTool?)null);
        _nuGetClientMock.Setup(x => x.GetLatestVersionAsync("my.tool", It.IsAny<CancellationToken>()))
            .ReturnsAsync("1.0.0");
        _toolRunnerMock.Setup(x => x.UpdateToolAsync("my.tool", It.IsAny<CancellationToken>()))
            .Callback(() => downloadStarted.TrySetResult(true))
            .ReturnsAsync(true);
        _toolRunnerMock.Setup(x => x.ExecuteAsync(toolSpec, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.ExecuteAsync(toolSpec, []);

        // Wait for background download to complete
        var started = await Task.WhenAny(downloadStarted.Task, Task.Delay(1000)) == downloadStarted.Task;
        started.Should().BeTrue("background download should have started");
    }
}
