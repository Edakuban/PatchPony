using System.Diagnostics;
using System.Text;

namespace PatchPony.Infrastructure.Git;

public sealed record GitCommand(string? WorkingDirectory, IReadOnlyList<string> Arguments);

public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError);

public interface IGitCommandRunner
{
    Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default);
}

public sealed class ProcessGitCommandRunner(string executable, TimeSpan timeout, int outputLimitBytes) : IGitCommandRunner
{
    public async Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory))
        {
            startInfo.WorkingDirectory = command.WorkingDirectory;
        }

        startInfo.Environment.Clear();
        startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var outputTask = ReadLimitedAsync(process.StandardOutput, outputLimitBytes, timeoutSource.Token);
        var errorTask = ReadLimitedAsync(process.StandardError, outputLimitBytes, timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
            return new GitCommandResult(process.ExitCode, await outputTask, await errorTask);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException("The Git command exceeded its configured timeout.");
        }
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, int limitBytes, CancellationToken cancellationToken)
    {
        var buffer = new char[1024];
        var builder = new StringBuilder();
        var retainedBytes = 0;

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            for (var index = 0; index < read; index++)
            {
                var characterBytes = Encoding.UTF8.GetByteCount(buffer[index].ToString());
                if (retainedBytes + characterBytes > limitBytes)
                {
                    continue;
                }

                builder.Append(buffer[index]);
                retainedBytes += characterBytes;
            }
        }

        return builder.ToString();
    }

    private static void TryKill(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
