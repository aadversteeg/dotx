using System.Diagnostics;
using System.Text.RegularExpressions;
using Core.Application;
using Core.Domain;

namespace Core.Infrastructure.ConsoleApp.Services;

public partial class DotnetToolRunner : IToolRunner
{
    public async Task<string?> GetInstalledVersionAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("tool");
            startInfo.ArgumentList.Add("list");
            startInfo.ArgumentList.Add("-g");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return null;
            }

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var match = ToolListLineRegex().Match(line);
                if (match.Success)
                {
                    var id = match.Groups["packageId"].Value;
                    var version = match.Groups["version"].Value;

                    if (id.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                    {
                        return version;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateToolAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("tool");
            startInfo.ArgumentList.Add("update");
            startInfo.ArgumentList.Add("-g");
            startInfo.ArgumentList.Add(packageId);

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

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

    [GeneratedRegex(@"^(?<packageId>\S+)\s+(?<version>\S+)\s+(?<commands>.*)$")]
    private static partial Regex ToolListLineRegex();
}
