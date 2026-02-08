using Core.Domain;
using FluentAssertions;

namespace UnitTests.Domain;

public class SemanticVersionTests
{
    [Fact(DisplayName = "SEM-001: TryParse with valid version should succeed")]
    public void SEM001()
    {
        var success = SemanticVersion.TryParse("1.2.3", out var result);

        success.Should().BeTrue();
        result!.Major.Should().Be(1);
        result.Minor.Should().Be(2);
        result.Patch.Should().Be(3);
        result.Prerelease.Should().BeNull();
    }

    [Fact(DisplayName = "SEM-002: TryParse with prerelease should parse prerelease")]
    public void SEM002()
    {
        var success = SemanticVersion.TryParse("1.0.0-preview.1", out var result);

        success.Should().BeTrue();
        result!.Major.Should().Be(1);
        result.Minor.Should().Be(0);
        result.Patch.Should().Be(0);
        result.Prerelease.Should().Be("preview.1");
    }

    [Fact(DisplayName = "SEM-003: TryParse with major only should default minor and patch")]
    public void SEM003()
    {
        var success = SemanticVersion.TryParse("1", out var result);

        success.Should().BeTrue();
        result!.Major.Should().Be(1);
        result.Minor.Should().Be(0);
        result.Patch.Should().Be(0);
    }

    [Fact(DisplayName = "SEM-004: TryParse with major.minor should default patch")]
    public void SEM004()
    {
        var success = SemanticVersion.TryParse("1.2", out var result);

        success.Should().BeTrue();
        result!.Major.Should().Be(1);
        result.Minor.Should().Be(2);
        result.Patch.Should().Be(0);
    }

    [Fact(DisplayName = "SEM-005: TryParse with null should fail")]
    public void SEM005()
    {
        var success = SemanticVersion.TryParse(null, out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SEM-006: TryParse with empty should fail")]
    public void SEM006()
    {
        var success = SemanticVersion.TryParse("", out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SEM-007: TryParse with invalid should fail")]
    public void SEM007()
    {
        var success = SemanticVersion.TryParse("abc", out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SEM-008: CompareTo with greater major should return positive")]
    public void SEM008()
    {
        SemanticVersion.TryParse("2.0.0", out var v1);
        SemanticVersion.TryParse("1.9.9", out var v2);

        (v1! > v2!).Should().BeTrue();
    }

    [Fact(DisplayName = "SEM-009: CompareTo with greater minor should return positive")]
    public void SEM009()
    {
        SemanticVersion.TryParse("1.2.0", out var v1);
        SemanticVersion.TryParse("1.1.9", out var v2);

        (v1! > v2!).Should().BeTrue();
    }

    [Fact(DisplayName = "SEM-010: CompareTo with greater patch should return positive")]
    public void SEM010()
    {
        SemanticVersion.TryParse("1.0.2", out var v1);
        SemanticVersion.TryParse("1.0.1", out var v2);

        (v1! > v2!).Should().BeTrue();
    }

    [Fact(DisplayName = "SEM-011: CompareTo with equal versions should return zero")]
    public void SEM011()
    {
        SemanticVersion.TryParse("1.0.0", out var v1);
        SemanticVersion.TryParse("1.0.0", out var v2);

        v1!.CompareTo(v2).Should().Be(0);
    }

    [Fact(DisplayName = "SEM-012: Release should be greater than prerelease")]
    public void SEM012()
    {
        SemanticVersion.TryParse("1.0.0", out var release);
        SemanticVersion.TryParse("1.0.0-preview.1", out var prerelease);

        (release! > prerelease!).Should().BeTrue();
    }

    [Fact(DisplayName = "SEM-013: ToString should format correctly")]
    public void SEM013()
    {
        SemanticVersion.TryParse("1.2.3", out var result);

        result!.ToString().Should().Be("1.2.3");
    }

    [Fact(DisplayName = "SEM-014: ToString with prerelease should include prerelease")]
    public void SEM014()
    {
        SemanticVersion.TryParse("1.0.0-beta.1", out var result);

        result!.ToString().Should().Be("1.0.0-beta.1");
    }
}
