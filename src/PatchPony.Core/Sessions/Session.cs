using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Sessions;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId New() => new(Guid.NewGuid());
}

public enum SessionStatus
{
    Created,
    Provisioning,
    Active,
    Closing,
    Closed,
    Expired,
    Failed
}

public sealed record Session(
    SessionId Id,
    ProjectId ProjectId,
    JobId JobId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    SessionStatus Status = SessionStatus.Created,
    DateTimeOffset? StatusChangedAt = null,
    string? FailureCode = null)
{
    public bool IsTerminal => Status is SessionStatus.Closed or SessionStatus.Expired or SessionStatus.Failed;

    public static Result<Session> Create(
        SessionId id,
        ProjectId projectId,
        JobId jobId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (id.Value == Guid.Empty || projectId.Value == Guid.Empty || jobId.Value == Guid.Empty || expiresAt <= createdAt)
        {
            return Result<Session>.Failure(DomainError.Validation("Session identifiers and a future expiry time are required."));
        }

        if (!SessionLimits.HasAllowedDuration(createdAt, expiresAt))
        {
            return Result<Session>.Failure(new DomainError("session.duration.exceeded", "The requested session duration exceeds the server-side maximum."));
        }
        return Result<Session>.Success(new Session(id, projectId, jobId, createdAt, expiresAt, SessionStatus.Created, createdAt));
    }

    public Result<Session> Transition(SessionStatus target, DateTimeOffset occurredAt, string? failureCode = null)
    {
        if (occurredAt < (StatusChangedAt ?? CreatedAt))
        {
            return Result<Session>.Failure(DomainError.Validation("Session transition time must not move backwards."));
        }

        if (target == Status)
        {
            return Result<Session>.Failure(new DomainError("session.transition.noop", "The session is already in the requested state."));
        }

        if (target == SessionStatus.Expired && occurredAt < ExpiresAt)
        {
            return Result<Session>.Failure(new DomainError("session.expiry.not_reached", "The session expiry time has not been reached."));
        }

        if (!CanTransition(Status, target))
        {
            return Result<Session>.Failure(new DomainError("session.transition.invalid", "The requested session state transition is not allowed."));
        }

        if (target == SessionStatus.Failed && !IsSafeFailureCode(failureCode))
        {
            return Result<Session>.Failure(DomainError.Validation("A bounded failure code is required when a session fails."));
        }

        if (target != SessionStatus.Failed && failureCode is not null)
        {
            return Result<Session>.Failure(DomainError.Validation("A failure code is only allowed for failed sessions."));
        }

        return Result<Session>.Success(this with
        {
            Status = target,
            StatusChangedAt = occurredAt,
            FailureCode = target == SessionStatus.Failed ? failureCode : null
        });
    }

    private static bool CanTransition(SessionStatus from, SessionStatus to) => (from, to) switch
    {
        (SessionStatus.Created, SessionStatus.Provisioning or SessionStatus.Closing) => true,
        (SessionStatus.Provisioning, SessionStatus.Active or SessionStatus.Closing or SessionStatus.Failed) => true,
        (SessionStatus.Active, SessionStatus.Closing or SessionStatus.Expired or SessionStatus.Failed) => true,
        (SessionStatus.Closing, SessionStatus.Closed or SessionStatus.Failed) => true,
        (SessionStatus.Failed or SessionStatus.Expired, SessionStatus.Closing) => true,
        _ => false
    };

    private static bool IsSafeFailureCode(string? failureCode) =>
        !string.IsNullOrWhiteSpace(failureCode) &&
        failureCode.Length <= 64 &&
        failureCode.All(character => char.IsLower(character) || char.IsDigit(character) || character is '.' or '_' or '-');
}