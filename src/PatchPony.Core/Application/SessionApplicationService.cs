using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Application;

public sealed class SessionApplicationService(IProjectRepository projects, IJobRepository jobs, ISessionRepository sessions)
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
}
