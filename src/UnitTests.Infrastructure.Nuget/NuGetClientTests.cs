using Core.Infrastructure.Nuget;
using FluentAssertions;

namespace UnitTests.Infrastructure.Nuget;

public class NuGetClientTests : IDisposable
{
    private readonly NuGetClient _sut = new();
    private readonly string _testCacheDir;
    private readonly string? _originalNuGetPackages;

    public NuGetClientTests()
    {
        // Set up isolated cache directory for download tests
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"dotx-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testCacheDir);
        _originalNuGetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        Environment.SetEnvironmentVariable("NUGET_PACKAGES", _testCacheDir);
    }

    public void Dispose()
    {
        // Restore original environment and clean up
        Environment.SetEnvironmentVariable("NUGET_PACKAGES", _originalNuGetPackages);
        if (Directory.Exists(_testCacheDir))
        {
            Directory.Delete(_testCacheDir, recursive: true);
        }
    }

    [Fact(DisplayName = "NUG-001: GetLatestVersionAsync with valid package should return version")]
    public async Task NUG001()
    {
        // Using a well-known stable package
        var result = await _sut.GetLatestVersionAsync("Newtonsoft.Json");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "NUG-002: GetLatestVersionAsync with nonexistent package should return null")]
    public async Task NUG002()
    {
        var result = await _sut.GetLatestVersionAsync("this.package.definitely.does.not.exist.12345");

        result.Should().BeNull();
    }

    [Fact(DisplayName = "NUG-003: GetLatestVersionAsync should be case insensitive")]
    public async Task NUG003()
    {
        var result1 = await _sut.GetLatestVersionAsync("newtonsoft.json");
        var result2 = await _sut.GetLatestVersionAsync("NEWTONSOFT.JSON");

        result1.Should().Be(result2);
    }

    [Fact(DisplayName = "NUG-004: GetLatestVersionAsync with cancellation should not throw")]
    public async Task NUG004()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.GetLatestVersionAsync("Newtonsoft.Json", cts.Token);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "NUG-005: DownloadPackageAsync with specific version should download to cache")]
    public async Task NUG005()
    {
        var result = await _sut.DownloadPackageAsync("Newtonsoft.Json", "13.0.1");

        result.Should().Be("13.0.1");

        var packageDir = Path.Combine(_testCacheDir, "newtonsoft.json", "13.0.1");
        Directory.Exists(packageDir).Should().BeTrue();
        File.Exists(Path.Combine(packageDir, "newtonsoft.json.13.0.1.nupkg")).Should().BeTrue();
        File.Exists(Path.Combine(packageDir, ".nupkg.metadata")).Should().BeTrue();
    }

    [Fact(DisplayName = "NUG-006: DownloadPackageAsync without version should download latest")]
    public async Task NUG006()
    {
        var latestVersion = await _sut.GetLatestVersionAsync("Newtonsoft.Json");
        latestVersion.Should().NotBeNull();

        var result = await _sut.DownloadPackageAsync("Newtonsoft.Json");

        result.Should().Be(latestVersion);
    }

    [Fact(DisplayName = "NUG-007: DownloadPackageAsync with nonexistent package should return null")]
    public async Task NUG007()
    {
        var result = await _sut.DownloadPackageAsync("this.package.definitely.does.not.exist.12345", "1.0.0");

        result.Should().BeNull();
    }

    [Fact(DisplayName = "NUG-008: DownloadPackageAsync with already cached package should return version")]
    public async Task NUG008()
    {
        // First download
        var result1 = await _sut.DownloadPackageAsync("Newtonsoft.Json", "13.0.1");
        result1.Should().Be("13.0.1");

        // Second download should return immediately (already cached)
        var result2 = await _sut.DownloadPackageAsync("Newtonsoft.Json", "13.0.1");
        result2.Should().Be("13.0.1");
    }

    [Fact(DisplayName = "NUG-009: DownloadPackageAsync should be case insensitive")]
    public async Task NUG009()
    {
        var result = await _sut.DownloadPackageAsync("NEWTONSOFT.JSON", "13.0.1");

        result.Should().Be("13.0.1");

        // Package directory should use lowercase
        var packageDir = Path.Combine(_testCacheDir, "newtonsoft.json", "13.0.1");
        Directory.Exists(packageDir).Should().BeTrue();
    }

    [Fact(DisplayName = "NUG-010: DownloadPackageAsync with cancellation should return null")]
    public async Task NUG010()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.DownloadPackageAsync("Newtonsoft.Json", "13.0.1", cts.Token);

        result.Should().BeNull();
    }
}
