using Core.Domain;
using FluentAssertions;

namespace UnitTests.Domain;

public class InstalledToolTests
{
    [Fact(DisplayName = "ITL-001: InstalledTool should store package id correctly")]
    public void ITL001()
    {
        var tool = new InstalledTool("my.package", "1.0.0", "my-command");

        tool.PackageId.Should().Be("my.package");
    }

    [Fact(DisplayName = "ITL-002: InstalledTool should store version correctly")]
    public void ITL002()
    {
        var tool = new InstalledTool("my.package", "1.2.3", "my-command");

        tool.Version.Should().Be("1.2.3");
    }

    [Fact(DisplayName = "ITL-003: InstalledTool should store commands correctly")]
    public void ITL003()
    {
        var tool = new InstalledTool("my.package", "1.0.0", "my-command");

        tool.Commands.Should().Be("my-command");
    }

    [Fact(DisplayName = "ITL-004: InstalledTool equality should compare all properties")]
    public void ITL004()
    {
        var tool1 = new InstalledTool("my.package", "1.0.0", "my-command");
        var tool2 = new InstalledTool("my.package", "1.0.0", "my-command");

        tool1.Should().Be(tool2);
    }

    [Fact(DisplayName = "ITL-005: InstalledTool with different package id should not be equal")]
    public void ITL005()
    {
        var tool1 = new InstalledTool("my.package", "1.0.0", "my-command");
        var tool2 = new InstalledTool("other.package", "1.0.0", "my-command");

        tool1.Should().NotBe(tool2);
    }

    [Fact(DisplayName = "ITL-006: InstalledTool with different version should not be equal")]
    public void ITL006()
    {
        var tool1 = new InstalledTool("my.package", "1.0.0", "my-command");
        var tool2 = new InstalledTool("my.package", "2.0.0", "my-command");

        tool1.Should().NotBe(tool2);
    }

    [Fact(DisplayName = "ITL-007: InstalledTool with different commands should not be equal")]
    public void ITL007()
    {
        var tool1 = new InstalledTool("my.package", "1.0.0", "command1");
        var tool2 = new InstalledTool("my.package", "1.0.0", "command2");

        tool1.Should().NotBe(tool2);
    }
}
