using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Queue;

namespace PatchPony.Core.Application;

public sealed class JobQueueService(IJobRepository jobs, IJobQueue queue)
{
    public async Task<Result<bool>> EnqueueAsync(
        JobId jobId,
        DateTimeOffset availableAt,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default)
    {
        if (jobId.Value == Guid.Empty || availableAt < enqueuedAt)
        {
            return Result<bool>.Failure(DomainError.Validation("A job identifier and an availability time on or after the enqueue time are required."));
        }

        if (await jobs.GetAsync(jobId, cancellationToken) is null)
        {
            return Result<bool>.Failure(new DomainError("job.not_found", "The job does not exist."));
        }

        var inserted = await queue.TryEnqueueAsync(new JobQueueEntry(jobId, enqueuedAt, availableAt), cancellationToken);
        return Result<bool>.Success(inserted);
    }

    public Task<Result<JobClaim?>> ClaimNextAsync(
        string workerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > 255 || leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromHours(1))
        {
            return Task.FromResult(Result<JobClaim?>.Failure(DomainError.Validation("A worker identifier of at most 255 characters and a lease duration up to one hour are required.")));
        }

        return ClaimAsync(workerId.Trim(), now, leaseDuration, cancellationToken);
    }

    private async Task<Result<JobClaim?>> ClaimAsync(string workerId, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        var claim = await queue.TryClaimAsync(workerId, now, leaseDuration, cancellationToken);
        return Result<JobClaim?>.Success(claim);
    }

    public Task<IReadOnlyList<JobQueueEntry>> GetReadyAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), "The maximum count must be between 1 and 100.");
        }

        return queue.GetReadyAsync(now, maximumCount, cancellationToken);
    }
}
