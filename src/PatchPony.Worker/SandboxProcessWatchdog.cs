using System.Diagnostics;

namespace PatchPony.Worker;

public interface ISandboxProcessTerminator
{
    void Terminate(Process process);
}

public sealed class SandboxProcessTerminator : ISandboxProcessTerminator
{
    public void Terminate(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }
}

public interface ISandboxProcessWatchdog
{
    void Watch(Process process, TimeSpan timeout, Action? onTimeout = null);
}

/// <summary>Fails closed by terminating the Docker client process after the server-defined execution timeout.</summary>
public sealed class SandboxProcessWatchdog(ISandboxProcessTerminator terminator) : ISandboxProcessWatchdog
{
    public void Watch(Process process, TimeSpan timeout, Action? onTimeout = null) => _ = StopAfterTimeoutAsync(process, timeout, onTimeout);

    private async Task StopAfterTimeoutAsync(Process process, TimeSpan timeout, Action? onTimeout)
    {
        try
        {
            await Task.Delay(timeout).ConfigureAwait(false);
            if (process.HasExited) return;

            terminator.Terminate(process);
            onTimeout?.Invoke();
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }
}