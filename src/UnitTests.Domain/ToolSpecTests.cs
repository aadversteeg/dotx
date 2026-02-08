using Core.Domain;
using FluentAssertions;

namespace UnitTests.Domain;

public class ToolSpecTests
{
    [Fact(DisplayName = "TSP-001: Parse with simple name should return name and no version")]
    public void TSP001()
    {
        var result = ToolSpec.Parse("ave.mcpserver.chronos");

        result.PackageId.Should().Be("ave.mcpserver.chronos");
        result.Version.Should().BeNull();
        result.IsPinned.Should().BeFalse();
    }

    [Fact(DisplayName = "TSP-002: Parse with version should return name and version")]
    public void TSP002()
    {
        var result = ToolSpec.Parse("ave.mcpserver.chronos@1.0.0");

        result.PackageId.Should().Be("ave.mcpserver.chronos");
        result.Version.Should().Be("1.0.0");
        result.IsPinned.Should().BeTrue();
    }

    [Fact(DisplayName = "TSP-003: Parse with prerelease version should return full version")]
    public void TSP003()
    {
        var result = ToolSpec.Parse("ave.mcpserver.chronos@1.0.0-preview.1");

        result.PackageId.Should().Be("ave.mcpserver.chronos");
        result.Version.Should().Be("1.0.0-preview.1");
        result.IsPinned.Should().BeTrue();
    }

    [Fact(DisplayName = "TSP-004: Parse with null should throw ArgumentException")]
    public void TSP004()
    {
        var act = () => ToolSpec.Parse(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "TSP-005: Parse with empty string should throw ArgumentException")]
    public void TSP005()
    {
        var act = () => ToolSpec.Parse("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "TSP-006: Parse with whitespace should throw ArgumentException")]
    public void TSP006()
    {
        var act = () => ToolSpec.Parse("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "TSP-007: ToString without version should return package id")]
    public void TSP007()
    {
        var result = ToolSpec.Parse("ave.mcpserver.chronos");

        result.ToString().Should().Be("ave.mcpserver.chronos");
    }

    [Fact(DisplayName = "TSP-008: ToString with version should return package id and version")]
    public void TSP008()
    {
        var result = ToolSpec.Parse("ave.mcpserver.chronos@1.0.0");

        result.ToString().Should().Be("ave.mcpserver.chronos@1.0.0");
    }
}
