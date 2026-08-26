using PatchPony.Core.Persistence;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Application;

public sealed record SessionCleanupFailure(SessionId SessionId, string ErrorCode);

public sealed record SessionCleanupResult(
    int Examined,
    IReadOnlyList<SessionId> CleanedSessionIds,
    IReadOnlyList<SessionCleanupFailure> Failures);

/// <summary>Bounded, retry-safe cleanup of sessions whose configured expiry has passed.</summary>
public sealed class SessionExpiryCleanupService(
    ISessionRepository sessions,
    ISessionWorkspaceDiscarder discarder,
    ISessionBaseCheckoutResolver checkouts)
{
    public async Task<SessionCleanupResult> RunAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken = default)
    {
        if (maximumCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), "The cleanup batch size must be between 1 and 100.");
        }

        var due = await sessions.GetDueForCleanupAsync(now, maximumCount, cancellationToken);
        var cleaned = new List<SessionId>();
        var failures = new List<SessionCleanupFailure>();
        foreach (var session in due)
        {
            var outcome = await CleanupAsync(session, now, cancellationToken);
            if (outcome is null)
            {
                cleaned.Add(session.Id);
            }
            else
            {
                failures.Add(new SessionCleanupFailure(session.Id, outcome));
            }
        }

        return new SessionCleanupResult(due.Count, cleaned, failures);
    }

    private async Task<string?> CleanupAsync(Session session, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = session;
        if (current.Status == SessionStatus.Active)
        {
            var expired = current.Transition(SessionStatus.Expired, now);
            if (!expired.IsSuccess)
            {
                return expired.Error.Code;
            }

            current = expired.Value!;
            await sessions.UpdateAsync(current, cancellationToken);
        }

        if (current.Status != SessionStatus.Closing)
        {
            var closing = current.Transition(SessionStatus.Closing, now);
            if (!closing.IsSuccess)
            {
                return closing.Error.Code;
            }

            current = closing.Value!;
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