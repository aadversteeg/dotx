# dotx

Execute .NET tools without installation - optimized for MCP servers. The .NET equivalent of npx/uvx.

## Features

- **Offline mode**: Runs cached tools directly without network access
- **Auto-install**: Automatically installs tools on first use via `dotnet tool exec -y`
- **Auto-update**: Checks for newer versions on NuGet in the background
- **Version pinning**: Pin to specific versions with `@version` syntax to skip updates
- **MCP compatible**: Clean stdio passthrough for Model Context Protocol servers
- **Cross-platform**: Works on Windows, macOS, and Linux

## Installation

```bash
dotnet tool install -g Ave.DotnetTool.DotX
```

## Usage

```bash
# Execute a tool (runs from cache, auto-updates in background)
dotx ave.mcpserver.chronos

# Pin to a specific version (no auto-update)
dotx ave.mcpserver.chronos@1.0.0

# Skip update check (pure offline mode)
dotx --no-update ave.mcpserver.chronos

# Pass arguments to the tool
dotx ave.mcpserver.chronos --some-arg value

# Show help
dotx --help
```

## MCP Server Configuration

Configure in Claude Desktop or other MCP clients:

```json
{
  "mcpServers": {
    "chronos": {
      "command": "dotx",
      "args": ["ave.mcpserver.chronos"],
      "env": {
        "DefaultTimeZoneId": "Europe/Amsterdam"
      }
    }
  }
}
```

## How It Works

1. **Parse tool spec**: Extracts tool name and optional version from `toolname@version`
2. **Check cache**: Looks for the tool in the NuGet packages cache (`~/.nuget/packages`)
3. **Execute from cache**: If cached, runs the tool directly via `dotnet <dll-path>` (no network required)
4. **Auto-install if needed**: If not cached, falls back to `dotnet tool exec -y` for auto-installation
5. **Background update**: Checks NuGet for newer versions and downloads for next run (unless pinned or `--no-update`)

### Why dotx over `dotnet tool exec -y`?

- **Works offline**: `dotnet tool exec -y` always requires network, even for cached tools. dotx runs directly from cache.
- **Faster startup**: Running from cache skips the NuGet resolution step.
- **Simpler syntax**: `dotx tool` vs `dotnet tool exec -y tool`
- **Cache management**: Built-in commands to list, show, and remove cached tools.

## Options

| Option | Description |
| ------ | ----------- |
| `--no-update` | Skip checking for updates (pure offline mode) |
| `--verbose` | Show detailed output |
| `--help`, `-h` | Show help message |
| `--version`, `-v` | Show version information |

## Cache Management

Tools are cached in the NuGet packages cache (typically `~/.nuget/packages` on Linux/macOS or `%USERPROFILE%\.nuget\packages` on Windows).

```bash
# List all cached .NET tools
dotx cache list

# Show details for a specific tool (including all cached versions)
dotx cache show ave.mcpserver.chronos

# Download a tool to cache (for offline use)
dotx cache add ave.mcpserver.chronos

# Download a specific version to cache
dotx cache add ave.mcpserver.chronos@1.0.0

# Update all cached tools to latest version
dotx cache update

# Update a specific tool to latest version
dotx cache update ave.mcpserver.chronos

# Remove a specific tool from cache
dotx cache remove ave.mcpserver.chronos

# Remove all .NET tools from cache (with confirmation)
dotx cache clear

# Remove all .NET tools from cache (skip confirmation)
dotx cache clear -y
```

### Cache Commands Reference

| Command | Description |
| ------- | ----------- |
| `cache list` | List all installed tools |
| `cache show <id>` | Show details for a specific tool |
| `cache add <id>[@ver]` | Download a tool to cache |
| `cache update [<id>]` | Update a tool (or all) to latest |
| `cache remove <id>` | Remove a specific tool |
| `cache clear [-y]` | Remove all tools |

Note: The cache commands only affect .NET tools (packages with `DotnetTool` package type), not other NuGet packages.

## Requirements

- .NET 10.0 or later

## License

MIT
