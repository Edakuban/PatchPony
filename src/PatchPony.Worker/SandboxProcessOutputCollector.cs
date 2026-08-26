using System.Diagnostics;
using System.Text;
using PatchPony.Core.Sandbox;

namespace PatchPony.Worker;

public interface ISandboxProcessOutputCollector
{
    SandboxProcessOutputCapture Capture(Process process, long maxBytesPerStream, TimeSpan timeout);
}

/// <summary>Drains both redirected streams while retaining only server-bounded output.</summary>
public sealed class SandboxProcessOutputCollector : ISandboxProcessOutputCollector
{
    public SandboxProcessOutputCapture Capture(Process process, long maxBytesPerStream, TimeSpan timeout) =>
        new(CaptureAsync(process, maxBytesPerStream, timeout));

    private static async Task<SandboxProcessOutput> CaptureAsync(Process process, long maxBytesPerStream, TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource();
        var stdout = ReadBoundedAsync(process.StandardOutput.BaseStream, maxBytesPerStream, timeoutSource.Token);
        var stderr = ReadBoundedAsync(process.StandardError.BaseStream, maxBytesPerStream, timeoutSource.Token);
        var exited = process.WaitForExitAsync();
        var timeoutElapsed = await Task.WhenAny(exited, Task.Delay(timeout)).ConfigureAwait(false) != exited;
        if (timeoutElapsed) timeoutSource.Cancel();

        var captured = await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        return new SandboxProcessOutput(captured[0].Text, captured[1].Text, captured[0].Truncated, captured[1].Truncated, timeoutElapsed, timeoutElapsed ? null : process.ExitCode);
    }

    private static async Task<BoundedOutput> ReadBoundedAsync(Stream stream, long maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var retained = new MemoryStream();
        var truncated = false;
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                var remaining = maxBytes - retained.Length;
                if (remaining > 0)
                {
                    var retainedBytes = (int)Math.Min(remaining, read);
                    retained.Write(buffer, 0, retainedBytes);
                    truncated |= retainedBytes < read;
                }
                else
                {
                    truncated = true;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            truncated = true;
        }

        return new BoundedOutput(Encoding.UTF8.GetString(retained.ToArray()), truncated);
    }

    private sealed record BoundedOutput(string Text, bool Truncated);
}

public sealed record SandboxOutputLimits(long MaxBytesPerStream);

public sealed class SandboxOutputLimitOptions
{
    public const string SectionName = "PatchPony:Worker:SandboxOutput";
    public long MaxBytesPerStream { get; init; }

    public SandboxOutputLimits CreateLimits()
    {
        if (MaxBytesPerStream is < 4 * 1024 or > 16L * 1024 * 1024)
        {
            throw new InvalidOperationException("Sandbox output byte limit must be between 4 KiB and 16 MiB per stream.");
        }

        return new SandboxOutputLimits(MaxBytesPerStream);
    }
}