using PatchPony.Core.Retention;

namespace PatchPony.Core.Application;

public sealed class RetentionService(IRetentionPreview retention)
{
    public Task<IReadOnlyList<RetentionCandidate>> PreviewExpiredAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount is < 1 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), "The maximum count must be between 1 and 1000.");
        }

        return retention.GetExpiredAsync(now, maximumCount, cancellationToken);
    }
}
