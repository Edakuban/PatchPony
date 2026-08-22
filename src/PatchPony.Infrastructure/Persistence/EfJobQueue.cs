using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Queue;

namespace PatchPony.Infrastructure.Persistence;

public sealed partial class EfJobQueue : IJobQueue
{
    private readonly PatchPonyDbContext database;

    public EfJobQueue(PatchPonyDbContext database)
    {
        this.database = database;
    }

    public async Task<bool> TryEnqueueAsync(JobQueueEntry entry, CancellationToken cancellationToken = default)
    {
        if (await database.JobQueue.AsNoTracking().AnyAsync(item => item.JobId == entry.JobId.Value, cancellationToken))
        {
            return false;
        }

        database.JobQueue.Add(new JobQueueItemRow
        {
            JobId = entry.JobId.Value,
            EnqueuedAt = entry.EnqueuedAt,
            AvailableAt = entry.AvailableAt
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            database.ChangeTracker.Clear();
            if (await database.JobQueue.AsNoTracking().AnyAsync(item => item.JobId == entry.JobId.Value, cancellationToken))
            {
                return false;
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<JobQueueEntry>> GetReadyAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken = default) =>
        await database.JobQueue.AsNoTracking()
            .Where(item => item.AvailableAt <= now && (item.ClaimExpiresAt == null || item.ClaimExpiresAt <= now))
            .OrderBy(item => item.AvailableAt)
            .ThenBy(item => item.EnqueuedAt)
            .Take(maximumCount)
            .Select(item => new JobQueueEntry(new JobId(item.JobId), item.EnqueuedAt, item.AvailableAt))
            .ToListAsync(cancellationToken);
}
