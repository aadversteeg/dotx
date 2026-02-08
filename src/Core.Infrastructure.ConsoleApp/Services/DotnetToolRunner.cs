using System.Diagnostics;
using Core.Application;
using Core.Domain;

namespace Core.Infrastructure.ConsoleApp.Services;

/// <summary>
/// Implements <see cref="IToolRunner"/> using the dotnet CLI.
/// </summary>
public class DotnetToolRunner : IToolRunner
{
    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(ToolSpec toolSpec, string[] args, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("tool");
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add(toolSpec.ToString());

        if (args.Length > 0)
        {
            startInfo.ArgumentList.Add("--");
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return 1;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    /// <inheritdoc/>
    public async Task<int> ExecuteFromCacheAsync(string dllPath, string[] args, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(dllPath);

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return 1;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
