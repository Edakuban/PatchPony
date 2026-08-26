using PatchPony.Core.Common;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Sandbox;

public sealed record SandboxContainerLaunchRequest(Guid ExecutionId, SessionId SessionId, SandboxCommand Command);
public sealed record SandboxContainerLaunch(int ProcessId, SandboxCommand Command, SandboxProcessOutputCapture? Output = null);

/// <summary>Worker-only process boundary for starting a server-composed sandbox container.</summary>
public interface ISandboxContainerLauncher
{
    Result<SandboxContainerLaunch> Start(SandboxContainerLaunchRequest request, CancellationToken cancellationToken = default);
}