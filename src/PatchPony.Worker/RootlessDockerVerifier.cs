using System.Diagnostics;

namespace PatchPony.Worker;

public interface IDockerRootlessProbe
{
    bool IsRootless();
}

/// <summary>Reads only Docker daemon security options with a fixed, shell-free command.</summary>
public sealed class DockerRootlessProbe : IDockerRootlessProbe
{
    public bool IsRootless()
    {
        try
        {
            var info = new ProcessStartInfo("docker") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add("info");
            info.ArgumentList.Add("--format");
            info.ArgumentList.Add("{{json .SecurityOptions}}");
            using var process = Process.Start(info);
            if (process is null || !process.WaitForExit(10_000) || process.ExitCode != 0) return false;
            return process.StandardOutput.ReadToEnd().Contains("name=rootless", StringComparison.Ordinal);
        }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}

public sealed class RootlessDockerOptions
{
    public const string SectionName = "PatchPony:Worker:RootlessDocker";
    public bool Required { get; init; } = true;

    public void Verify(IDockerRootlessProbe probe)
    {
        if (Required && !probe.IsRootless())
        {
            throw new InvalidOperationException("Sandbox execution requires a rootless Docker daemon.");
        }
    }
}