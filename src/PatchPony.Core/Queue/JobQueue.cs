using PatchPony.Core.Jobs;

namespace PatchPony.Core.Queue;

public sealed record JobQueueEntry(JobId JobId, DateTimeOffset EnqueuedAt, DateTimeOffset AvailableAt);

public sealed record JobClaim(Guid ClaimId, JobId JobId, string WorkerId, DateTimeOffset ExpiresAt);

public interface IJobQueue
{
    Task<bool> TryEnqueueAsync(JobQueueEntry entry, CancellationToken cancellationToken = default);

    Task<JobClaim?> TryClaimAsync(
        string workerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobQueueEntry>> GetReadyAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken = default);
}
