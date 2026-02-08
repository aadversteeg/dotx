# dotx

Execute .NET tools without installation - optimized for MCP servers. The .NET equivalent of npx/uvx.

## Features

- **Auto-install**: Automatically installs tools on first use via `dotnet tool exec -y`
- **Auto-update**: Checks for newer versions on NuGet and updates before execution
- **Version pinning**: Pin to specific versions with `@version` syntax to skip updates
- **MCP compatible**: Clean stdio passthrough for Model Context Protocol servers
- **Cross-platform**: Works on Windows, macOS, and Linux

## Installation

```bash
dotnet tool install -g Ave.DotnetTool.DotX
```

## Usage

```bash
# Execute a tool (auto-installs and auto-updates)
dotx ave.mcpserver.chronos

# Pin to a specific version (no auto-update)
dotx ave.mcpserver.chronos@1.0.0

# Skip update check
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
2. **Check for updates**: Queries NuGet API for latest version (skipped if version is pinned)
3. **Update if needed**: Runs `dotnet tool update -g` when newer version is available
4. **Execute tool**: Runs `dotnet tool exec -y <tool> -- <args>` with inherited stdio

## Options

| Option | Description |
|--------|-------------|
| `--no-update` | Skip checking for updates |
| `--verbose` | Show detailed output |
| `--help`, `-h` | Show help message |
| `--version`, `-v` | Show version information |

## Requirements

- .NET 10.0 or later

## License

MIT
