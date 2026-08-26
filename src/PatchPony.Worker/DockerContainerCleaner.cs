using System.Diagnostics;

namespace PatchPony.Worker;

public interface IDockerContainerCleaner
{
    void Remove(string containerName);
}

/// <summary>Best-effort hard cleanup for the server-generated sandbox container name.</summary>
public sealed class DockerContainerCleaner : IDockerContainerCleaner
{
    public void Remove(string containerName)
    {
        try
        {
            var info = new ProcessStartInfo("docker") { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("rm");
            info.ArgumentList.Add("--force");
            info.ArgumentList.Add(containerName);
            _ = Process.Start(info);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        catch (UnauthorizedAccessException) { }
    }
}