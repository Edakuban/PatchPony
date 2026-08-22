using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Workflows;

namespace PatchPony.Infrastructure.Persistence;

public sealed class EfIdempotentJobRepository(PatchPonyDbContext database) : IIdempotentJobRepository
{
    public async Task<IdempotentJobResult> GetOrCreateAsync(
        IdempotencyRecord record,
        Job newJob,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetExistingAsync(record.Key, cancellationToken);
        if (existing is not null)
        {
            return new IdempotentJobResult(existing, false);
        }

        database.Jobs.Add(new JobRow
        {
            Id = newJob.Id.Value,
            ProjectId = newJob.ProjectId.Value,
            Kind = newJob.Kind,
            Status = newJob.Status,
            CreatedAt = newJob.CreatedAt
        });
        database.IdempotencyRecords.Add(new IdempotencyRecordRow
        {
            Key = record.Key,
            ProjectId = record.ProjectId.Value,
            JobId = record.JobId.Value,
            CreatedAt = record.CreatedAt,
            ExpiresAt = record.ExpiresAt
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return new IdempotentJobResult(newJob, true);
        }
        catch (DbUpdateException)
        {
            database.ChangeTracker.Clear();
            existing = await GetExistingAsync(record.Key, cancellationToken);
            if (existing is not null)
            {
                return new IdempotentJobResult(existing, false);
            }

            throw;
        }
    }

    private async Task<Job?> GetExistingAsync(string key, CancellationToken cancellationToken)
    {
        var record = await database.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var job = await database.Jobs.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == record.JobId, cancellationToken);
        return job is null
            ? throw new InvalidOperationException("The idempotency record references a missing job.")
            : Job.Rehydrate(new JobId(job.Id), new ProjectId(job.ProjectId), job.Kind, job.Status, job.CreatedAt);
    }
}
