using Core.Application;
using Core.Domain;
using Core.Infrastructure.ConsoleApp.Services;
using Core.Infrastructure.Nuget;

namespace Core.Infrastructure.ConsoleApp;

/// <summary>
/// Entry point for the dotx CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the dotx CLI tool.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        if (args[0] == "--version" || args[0] == "-v")
        {
            PrintVersion();
            return 0;
        }

        // Handle cache commands
        if (args[0] == "cache")
        {
            return await HandleCacheCommandAsync(args.Skip(1).ToArray());
        }

        var skipUpdate = false;
        var verbose = false;
        var toolArgIndex = 0;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--no-update")
            {
                skipUpdate = true;
                toolArgIndex = i + 1;
            }
            else if (args[i] == "--verbose")
            {
                verbose = true;
                toolArgIndex = i + 1;
            }
            else
            {
                toolArgIndex = i;
                break;
            }
        }

        if (toolArgIndex >= args.Length)
        {
            await Console.Error.WriteLineAsync("Error: No tool specified.");
            PrintUsage();
            return 1;
        }

        var toolSpecString = args[toolArgIndex];
        var toolArgs = args.Skip(toolArgIndex + 1).ToArray();

        ToolSpec toolSpec;
        try
        {
            toolSpec = ToolSpec.Parse(toolSpecString);
        }
        catch (ArgumentException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }

        Action<string>? logAction = verbose ? msg => Console.Error.WriteLine(msg) : null;

        var nuGetClient = new NuGetClient();
        var toolRunner = new DotnetToolRunner();
        var cacheManager = new DotnetCacheManager();
        var executor = new ToolExecutor(nuGetClient, toolRunner, cacheManager, logAction);

        return await executor.ExecuteAsync(toolSpec, toolArgs, skipUpdate);
    }

    /// <summary>
    /// Handles cache subcommands (list, show, remove, clear).
    /// </summary>
    /// <param name="args">The arguments after 'cache'.</param>
    /// <returns>Exit code.</returns>
    private static async Task<int> HandleCacheCommandAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintCacheUsage();
            return 1;
        }

        var cacheManager = new DotnetCacheManager();

        return args[0] switch
        {
            "list" => await HandleCacheListAsync(cacheManager),
            "show" => await HandleCacheShowAsync(cacheManager, args.Skip(1).ToArray()),
            "remove" => await HandleCacheRemoveAsync(cacheManager, args.Skip(1).ToArray()),
            "clear" => await HandleCacheClearAsync(cacheManager, args.Skip(1).ToArray()),
            _ => PrintCacheUsageWithError($"Unknown cache command: {args[0]}")
        };
    }

    /// <summary>
    /// Handles 'dotx cache list' command.
    /// </summary>
    private static async Task<int> HandleCacheListAsync(ICacheManager cacheManager)
    {
        var tools = await cacheManager.ListToolsAsync();

        if (tools.Count == 0)
        {
            Console.WriteLine("No tools installed.");
            return 0;
        }

        Console.WriteLine($"{"Package Id",-40} {"Version",-15} Commands");
        Console.WriteLine(new string('-', 70));

        foreach (var tool in tools)
        {
            Console.WriteLine($"{tool.PackageId,-40} {tool.Version,-15} {tool.Commands}");
        }

        Console.WriteLine();
        Console.WriteLine($"{tools.Count} tool{(tools.Count == 1 ? "" : "s")} installed");

        return 0;
    }

    /// <summary>
    /// Handles 'dotx cache show <package-id>' command.
    /// </summary>
    private static async Task<int> HandleCacheShowAsync(ICacheManager cacheManager, string[] args)
    {
        if (args.Length == 0)
        {
            await Console.Error.WriteLineAsync("Error: Package ID required.");
            await Console.Error.WriteLineAsync("Usage: dotx cache show <package-id>");
            return 1;
        }

        var packageId = args[0];
        var versions = await cacheManager.GetToolVersionsAsync(packageId);

        if (versions.Count == 0)
        {
            Console.WriteLine($"Tool '{packageId}' is not installed.");
            return 1;
        }

        Console.WriteLine($"Package Id: {versions[0].PackageId}");
        Console.WriteLine($"Commands:   {versions[0].Commands}");
        Console.WriteLine($"Versions:   {string.Join(", ", versions.Select(v => v.Version))}");

        return 0;
    }

    /// <summary>
    /// Handles 'dotx cache remove <package-id>' command.
    /// </summary>
    private static async Task<int> HandleCacheRemoveAsync(ICacheManager cacheManager, string[] args)
    {
        if (args.Length == 0)
        {
            await Console.Error.WriteLineAsync("Error: Package ID required.");
            await Console.Error.WriteLineAsync("Usage: dotx cache remove <package-id>");
            return 1;
        }

        var packageId = args[0];
        var tool = await cacheManager.GetToolAsync(packageId);

        if (tool == null)
        {
            await Console.Error.WriteLineAsync($"Error: Tool '{packageId}' is not installed.");
            return 1;
        }

        var success = await cacheManager.RemoveToolAsync(packageId);

        if (success)
        {
            Console.WriteLine($"Removed {tool.PackageId} ({tool.Version})");
            return 0;
        }

        await Console.Error.WriteLineAsync($"Error: Failed to remove '{packageId}'.");
        return 1;
    }

    /// <summary>
    /// Handles 'dotx cache clear [-y]' command.
    /// </summary>
    private static async Task<int> HandleCacheClearAsync(ICacheManager cacheManager, string[] args)
    {
        var skipConfirmation = args.Contains("-y") || args.Contains("--yes");

        var tools = await cacheManager.ListToolsAsync();

        if (tools.Count == 0)
        {
            Console.WriteLine("No tools installed.");
            return 0;
        }

        if (!skipConfirmation)
        {
            Console.WriteLine($"This will remove {tools.Count} tool{(tools.Count == 1 ? "" : "s")}:");
            foreach (var tool in tools)
            {
                Console.WriteLine($"  - {tool.PackageId} ({tool.Version})");
            }
            Console.WriteLine();
            Console.Write("Continue? [y/N] ");

            var response = Console.ReadLine();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelled.");
                return 0;
            }
        }

        var removedCount = 0;
        foreach (var tool in tools)
        {
            if (await cacheManager.RemoveToolAsync(tool.PackageId))
            {
                Console.WriteLine($"Removed {tool.PackageId} ({tool.Version})");
                removedCount++;
            }
            else
            {
                await Console.Error.WriteLineAsync($"Failed to remove {tool.PackageId}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{removedCount} tool{(removedCount == 1 ? "" : "s")} removed");

        return 0;
    }

    /// <summary>
    /// Prints cache usage with an error message.
    /// </summary>
    private static int PrintCacheUsageWithError(string error)
    {
        Console.Error.WriteLine($"Error: {error}");
        PrintCacheUsage();
        return 1;
    }

    /// <summary>
    /// Prints the cache command usage information.
    /// </summary>
    private static void PrintCacheUsage()
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: dotx cache <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  list              List all installed tools");
        Console.Error.WriteLine("  show <id>         Show details for a specific tool");
        Console.Error.WriteLine("  remove <id>       Remove a specific tool");
        Console.Error.WriteLine("  clear [-y]        Remove all tools (use -y to skip confirmation)");
    }

    /// <summary>
    /// Prints the usage information to stderr.
    /// </summary>
    private static void PrintUsage()
    {
        Console.Error.WriteLine("dotx - Execute .NET tools without installation");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: dotx [options] <tool-name[@version]> [tool-args...]");
        Console.Error.WriteLine("       dotx cache <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --no-update    Skip checking for updates");
        Console.Error.WriteLine("  --verbose      Show detailed output");
        Console.Error.WriteLine("  --help, -h     Show this help message");
        Console.Error.WriteLine("  --version, -v  Show version information");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Cache Commands:");
        Console.Error.WriteLine("  cache list              List all installed tools");
        Console.Error.WriteLine("  cache show <id>         Show details for a specific tool");
        Console.Error.WriteLine("  cache remove <id>       Remove a specific tool");
        Console.Error.WriteLine("  cache clear [-y]        Remove all tools");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  dotx ave.mcpserver.chronos");
        Console.Error.WriteLine("  dotx ave.mcpserver.chronos@1.0.0");
        Console.Error.WriteLine("  dotx --no-update ave.mcpserver.chronos");
        Console.Error.WriteLine("  dotx cache list");
        Console.Error.WriteLine("  dotx cache show ave.mcpserver.chronos");
    }

    /// <summary>
    /// Prints the version information to stdout.
    /// </summary>
    private static void PrintVersion()
    {
        var version = typeof(Program).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion ?? "0.0.0";

        Console.WriteLine($"dotx {version}");
    }
}
