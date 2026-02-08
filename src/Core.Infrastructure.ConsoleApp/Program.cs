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
            var handler = new CacheCommandHandler(new NuGetClient(), new DotnetCacheManager(), Console.Out, Console.Error);
            return await handler.HandleAsync(args.Skip(1).ToArray());
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
        Console.Error.WriteLine("  cache add <id>[@ver]    Download a tool to cache");
        Console.Error.WriteLine("  cache update [<id>]     Update a tool (or all) to latest");
        Console.Error.WriteLine("  cache remove <id>       Remove a specific tool");
        Console.Error.WriteLine("  cache clear [-y]        Remove all tools");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Examples:");
        Console.Error.WriteLine("  dotx ave.mcpserver.chronos");
        Console.Error.WriteLine("  dotx ave.mcpserver.chronos@1.0.0");
        Console.Error.WriteLine("  dotx --no-update ave.mcpserver.chronos");
        Console.Error.WriteLine("  dotx cache list");
        Console.Error.WriteLine("  dotx cache add ave.mcpserver.chronos@1.0.0");
        Console.Error.WriteLine("  dotx cache update");
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
