using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Queue;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Sandbox;

/// <summary>Internal hand-off to a worker. Runner images, executables and arguments are deliberately absent.</summary>
public sealed record SandboxJobRequest(WorkerClaimProof Claim, SessionId SessionId, string CommandId)
{
    private static readonly Regex CommandIdPattern = new("^[a-z][a-z0-9._-]{0,127}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public Result ValidateContract()
    {
        if (Claim is null || SessionId.Value == Guid.Empty || !CommandIdPattern.IsMatch(CommandId ?? string.Empty))
        {
            return Result.Failure(DomainError.Validation("A signed worker claim, session identifier and registered command identifier are required."));
        }

        return Result.Success();
    }
}

public sealed record SandboxJobAcceptance(Guid ExecutionId, Guid ClaimId, SessionId SessionId, string CommandId, SandboxContainerLaunch Launch, DateTimeOffset AcceptedAt);

/// <summary>Private process boundary between the dispatcher and one worker; it is not a public Gateway API.</summary>
public interface ISandboxWorkerApi
{
    Result<SandboxJobAcceptance> Submit(SandboxJobRequest request, CancellationToken cancellationToken = default);
}