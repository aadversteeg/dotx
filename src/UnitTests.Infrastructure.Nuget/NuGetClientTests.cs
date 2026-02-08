using Core.Infrastructure.Nuget;
using FluentAssertions;

namespace UnitTests.Infrastructure.Nuget;

public class NuGetClientTests
{
    private readonly NuGetClient _sut = new();

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
}
