using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Application;

public sealed class JobApplicationService(IProjectRepository projects, IJobRepository jobs)
{
    public async Task<Result<Job>> CreateAsync(
        ProjectId projectId,
        string kind,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        if (await projects.GetAsync(projectId, cancellationToken) is null)
        {
            return Result<Job>.Failure(new DomainError("project.not_found", "The project does not exist."));
        }

        var jobResult = Job.Create(JobId.New(), projectId, kind, createdAt);
        if (!jobResult.IsSuccess)
        {
            return jobResult;
        }

        await jobs.AddAsync(jobResult.Value!, cancellationToken);
        return jobResult;
    }

    public async Task<Result<Job>> TransitionAsync(
        JobId jobId,
        JobStatus target,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Result<Job>.Failure(new DomainError("job.not_found", "The job does not exist."));
        }

        var transition = job.TransitionTo(target, occurredAt);
        if (!transition.IsSuccess)
        {
            return Result<Job>.Failure(transition.Error);
        }

        await jobs.UpdateAsync(job, cancellationToken);
        return Result<Job>.Success(job);
    }
}
