using PatchPony.Core.Common;
using PatchPony.Core.Queue;
using PatchPony.Core.Sandbox;

namespace PatchPony.Worker;

/// <summary>Authenticates internal sandbox jobs and starts only server-composed containers.</summary>
public sealed class SandboxWorkerApi(WorkerClaimAuthenticator claims, WorkerIdentity identity, ISandboxCommandCatalog commands, ISandboxContainerLauncher containers, ISandboxExecutionResultRecorder? results = null, TimeProvider? clock = null) : ISandboxWorkerApi
{
    private readonly TimeProvider clock = clock ?? TimeProvider.System;
    private readonly HashSet<Guid> consumedClaimIds = [];
    private readonly object sync = new();

    public Result<SandboxJobAcceptance> Submit(SandboxJobRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contract = request.ValidateContract();
        if (!contract.IsSuccess) return Result<SandboxJobAcceptance>.Failure(contract.Error);

        var claim = claims.Verify(request.Claim, identity.Id, clock.GetUtcNow());
        if (!claim.IsSuccess) return Result<SandboxJobAcceptance>.Failure(claim.Error);

        var command = commands.Resolve(request.CommandId);
        if (!command.IsSuccess) return Result<SandboxJobAcceptance>.Failure(command.Error);

        lock (sync)
        {
            if (consumedClaimIds.Contains(claim.Value!.ClaimId))
            {
                return Result<SandboxJobAcceptance>.Failure(DomainError.Conflict("sandbox.request.replayed", "The worker claim was already used for a sandbox request."));
            }

            var executionId = Guid.NewGuid();
            var launch = containers.Start(new SandboxContainerLaunchRequest(executionId, request.SessionId, command.Value!), cancellationToken);
            if (!launch.IsSuccess) return Result<SandboxJobAcceptance>.Failure(launch.Error);

            consumedClaimIds.Add(claim.Value.ClaimId);
            var acceptance = new SandboxJobAcceptance(
                executionId, claim.Value.ClaimId, request.SessionId, command.Value!.Id, launch.Value!, clock.GetUtcNow());
            results?.Track(acceptance);
            return Result<SandboxJobAcceptance>.Success(acceptance);
        }
    }
}