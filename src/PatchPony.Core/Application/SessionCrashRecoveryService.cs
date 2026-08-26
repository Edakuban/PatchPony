using PatchPony.Core.Common;
using PatchPony.Core.Persistence;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Application;

public sealed record SessionRecoveryFailure(SessionId SessionId, string ErrorCode);

public sealed record SessionRecoveryResult(
    int Examined,
    IReadOnlyList<SessionId> RecoveredSessionIds,
    IReadOnlyList<SessionRecoveryFailure> Failures);

/// <summary>Retries cleanup for sessions interrupted during provisioning or discard.</summary>
public sealed class SessionCrashRecoveryService(
    ISessionRepository sessions,
    ISessionWorkspaceDiscarder discarder,
    ISessionBaseCheckoutResolver checkouts)
{
    public async Task<SessionRecoveryResult> RunAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken = default)
    {
        if (maximumCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), "The recovery batch size must be between 1 and 100.");
        }

        var candidates = await sessions.GetForCrashRecoveryAsync(maximumCount, cancellationToken);
        var recovered = new List<SessionId>();
        var failures = new List<SessionRecoveryFailure>();
        foreach (var session in candidates)
        {
            var outcome = await RecoverAsync(session, now, cancellationToken);
            if (outcome is null)
            {
                recovered.Add(session.Id);
            }
            else
            {
                failures.Add(new SessionRecoveryFailure(session.Id, outcome));
            }
        }

        return new SessionRecoveryResult(candidates.Count, recovered, failures);
    }

    private async Task<string?> RecoverAsync(Session session, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var closing = session.Status == SessionStatus.Closing
            ? Result<Session>.Success(session)
            : session.Transition(SessionStatus.Closing, now);
        if (!closing.IsSuccess)
        {
            return closing.Error.Code;
        }

        var current = closing.Value!;
        if (current != session)
        {
            await sessions.UpdateAsync(current, cancellationToken);
        }

        var checkout = checkouts.Resolve(current.ProjectId);
        if (!checkout.IsSuccess)
        {
            return checkout.Error.Code;
        }

        var discarded = await discarder.DiscardAsync(current, checkout.Value!, cancellationToken);
        if (!discarded.IsSuccess)
        {
            return discarded.Error.Code;
        }

        var closed = current.Transition(SessionStatus.Closed, now);
        if (!closed.IsSuccess)
        {
            return closed.Error.Code;
        }

        await sessions.UpdateAsync(closed.Value!, cancellationToken);
        return null;
    }
}