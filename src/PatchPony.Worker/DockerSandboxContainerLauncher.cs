using System.Diagnostics;
using PatchPony.Core.Common;
using PatchPony.Core.Sandbox;

namespace PatchPony.Worker;

public interface IDockerProcessStarter
{
    Process? Start(ProcessStartInfo startInfo);
}

public sealed class ProcessDockerProcessStarter : IDockerProcessStarter
{
    public Process? Start(ProcessStartInfo startInfo) => Process.Start(startInfo);
}

/// <summary>Starts only server-composed Docker invocations; Docker arguments never originate from the sandbox request.</summary>
public sealed class DockerSandboxContainerLauncher(IDockerProcessStarter processes, SandboxSecurityProfile security, SandboxResourceLimits limits, ISandboxProcessWatchdog watchdog, ISandboxSessionWorktreeResolver worktrees, ISandboxProcessOutputCollector outputCollector, SandboxOutputLimits outputLimits, ISandboxProcessTerminator terminator, IDockerContainerCleaner cleaner) : ISandboxContainerLauncher
{
    public const string NonRootUser = "65532:65532";

    public Result<SandboxContainerLaunch> Start(SandboxContainerLaunchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var workspace = worktrees.Resolve(request.SessionId);
            if (!workspace.IsSuccess) return Result<SandboxContainerLaunch>.Failure(workspace.Error);

            var containerName = $"patchpony-sandbox-{request.ExecutionId:N}";



            var info = new ProcessStartInfo("docker") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add("run");
            info.ArgumentList.Add("--rm");

            info.ArgumentList.Add("--name");

            info.ArgumentList.Add(containerName);
            info.ArgumentList.Add("--user");
            info.ArgumentList.Add(NonRootUser);
            info.ArgumentList.Add("--read-only");

            info.ArgumentList.Add("--mount");

            info.ArgumentList.Add($"type=bind,src={workspace.Value!.HostPath},dst={SandboxSessionWorktree.ContainerPath},rw,bind-propagation=rprivate");
            info.ArgumentList.Add("--workdir");
            info.ArgumentList.Add(SandboxSessionWorktree.ContainerPath);
            info.ArgumentList.Add("--cpus");
            info.ArgumentList.Add(limits.CpuArgument);
            info.ArgumentList.Add("--memory");
            info.ArgumentList.Add(limits.MemoryBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
            info.ArgumentList.Add("--pids-limit");
            info.ArgumentList.Add(limits.PidsLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            info.ArgumentList.Add("--storage-opt");
            info.ArgumentList.Add($"size={limits.DiskBytes.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            info.ArgumentList.Add("--network");
            info.ArgumentList.Add("none");
            info.ArgumentList.Add("--cap-drop=ALL");
            info.ArgumentList.Add("--security-opt");
            info.ArgumentList.Add("no-new-privileges:true");
            info.ArgumentList.Add("--security-opt");
            info.ArgumentList.Add($"seccomp={security.SeccompProfilePath}");
            info.ArgumentList.Add("--security-opt");
            info.ArgumentList.Add($"apparmor={security.AppArmorProfile}");
            info.ArgumentList.Add(request.Command.Runner.ImageReference);
            info.ArgumentList.Add(request.Command.Executable);
            foreach (var argument in request.Command.Arguments) info.ArgumentList.Add(argument);

            var process = processes.Start(info);
            if (process is null)
            {
                return Result<SandboxContainerLaunch>.Failure(new DomainError("sandbox.container.start_failed", "The sandbox container could not be started."));
            }

            watchdog.Watch(process, limits.ExecutionTimeout, () => cleaner.Remove(containerName));
            var cancellation = cancellationToken.Register(() =>
            {
                terminator.Terminate(process);
                cleaner.Remove(containerName);
            });
            var output = outputCollector.Capture(process, outputLimits.MaxBytesPerStream, limits.ExecutionTimeout);
            _ = output.Completion.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return Result<SandboxContainerLaunch>.Success(new SandboxContainerLaunch(process.Id, request.Command, output));
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            return Result<SandboxContainerLaunch>.Failure(new DomainError("sandbox.container.start_failed", "The sandbox container could not be started."));
        }
    }
}