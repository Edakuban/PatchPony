using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Application;

public sealed class SessionApplicationService(IProjectRepository projects, IJobRepository jobs, ISessionRepository sessions, ISessionWorkspaceDiscarder? discarder = null)
{
    public async Task<Result<Session>> CreateAsync(
        ProjectId projectId,
        JobId jobId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (await projects.GetAsync(projectId, cancellationToken) is null)
        {
            return Result<Session>.Failure(new DomainError("project.not_found", "The project does not exist."));
        }

        var job = await jobs.GetAsync(jobId, cancellationToken);
        if (job is null || job.ProjectId != projectId)
        {
            return Result<Session>.Failure(new DomainError("job.not_found", "The job does not belong to the project."));
        }

        var sessionResult = Session.Create(SessionId.New(), projectId, jobId, createdAt, expiresAt);
        if (!sessionResult.IsSuccess)
        {
            return sessionResult;
        }

        await sessions.AddAsync(sessionResult.Value!, cancellationToken);
        return sessionResult;
    }

    public async Task<Result<Session>> DescribeAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetAsync(sessionId, cancellationToken);
        return session is null
            ? Result<Session>.Failure(new DomainError("session.not_found", "The requested session was not found."))
            : Result<Session>.Success(session);
    }
    public async Task<Result<Session>> DiscardAsync(
        SessionId sessionId,
        string baseCheckoutRoot,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        if (discarder is null)
        {
            return Result<Session>.Failure(new DomainError("session.discard.unavailable", "Session discard is not configured."));
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result<Session>.Failure(new DomainError("session.not_found", "The requested session was not found."));
        }

        if (session.Status is SessionStatus.Closed or SessionStatus.Expired)
        {
            return Result<Session>.Failure(new DomainError("session.discard.invalid_state", "A terminal session cannot be discarded."));
        }

        var closing = session.Status == SessionStatus.Closing
            ? Result<Session>.Success(session)
            : session.Transition(SessionStatus.Closing, occurredAt);
        if (!closing.IsSuccess)
        {
            return closing;
        }

        await sessions.UpdateAsync(closing.Value!, cancellationToken);
        var discarded = await discarder.DiscardAsync(closing.Value!, baseCheckoutRoot, cancellationToken);
        if (!discarded.IsSuccess)
        {
            return Result<Session>.Failure(discarded.Error);
        }

        var closed = closing.Value!.Transition(SessionStatus.Closed, occurredAt);
        if (!closed.IsSuccess)
        {
            return closed;
        }

        await sessions.UpdateAsync(closed.Value!, cancellationToken);
        return closed;
    }
    public async Task<Result<Session>> TransitionAsync(
        SessionId sessionId,
        SessionStatus target,
        DateTimeOffset occurredAt,
        string? failureCode = null,
        CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result<Session>.Failure(new DomainError("session.not_found", "The requested session was not found."));
        }

        var transitioned = session.Transition(target, occurredAt, failureCode);
        if (!transitioned.IsSuccess)
        {
            return transitioned;
        }

        await sessions.UpdateAsync(transitioned.Value!, cancellationToken);
        return transitioned;
    }}
