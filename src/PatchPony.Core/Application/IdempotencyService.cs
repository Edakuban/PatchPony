using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Workflows;

namespace PatchPony.Core.Application;

public sealed class IdempotencyService(IProjectRepository projects, IIdempotentJobRepository jobs)
{
    public async Task<Result<IdempotentJobResult>> CreateJobOnceAsync(
        ProjectId projectId,
        string idempotencyKey,
        string kind,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 255 || expiresAt <= createdAt)
        {
            return Result<IdempotentJobResult>.Failure(
                DomainError.Validation("An idempotency key of at most 255 characters and a future expiry time are required."));
        }

        if (await projects.GetAsync(projectId, cancellationToken) is null)
        {
            return Result<IdempotentJobResult>.Failure(new DomainError("project.not_found", "The project does not exist."));
        }

        var jobResult = Job.Create(JobId.New(), projectId, kind, createdAt);
        if (!jobResult.IsSuccess)
        {
            return Result<IdempotentJobResult>.Failure(jobResult.Error);
        }

        var job = jobResult.Value!;
        var record = new IdempotencyRecord(idempotencyKey.Trim(), projectId, job.Id, createdAt, expiresAt);
        var result = await jobs.GetOrCreateAsync(record, job, cancellationToken);
        return Result<IdempotentJobResult>.Success(result);
    }
}
