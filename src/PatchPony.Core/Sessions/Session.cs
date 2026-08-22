using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Sessions;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId New() => new(Guid.NewGuid());
}

public sealed record Session(SessionId Id, ProjectId ProjectId, JobId JobId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)
{
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

        return Result<Session>.Success(new Session(id, projectId, jobId, createdAt, expiresAt));
    }
}
