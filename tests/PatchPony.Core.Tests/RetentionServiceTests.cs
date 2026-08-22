using PatchPony.Core.Application;
using PatchPony.Core.Retention;

namespace PatchPony.Core.Tests;

public sealed class RetentionServiceTests
{
    [Fact]
    public async Task PreviewExpired_DelegatesWithoutProvidingAnyDeletionOperation()
    {
        var candidate = new RetentionCandidate(RetentionRecordType.Session, "session-42", DateTimeOffset.UtcNow.AddDays(-1));
        var service = new RetentionService(new FixedRetentionPreview(candidate));

        var result = await service.PreviewExpiredAsync(DateTimeOffset.UtcNow, 10);

        Assert.Single(result);
        Assert.Equal(candidate, result[0]);
    }

    [Fact]
    public async Task PreviewExpired_RejectsAnUnsafePageSize()
    {
        var service = new RetentionService(new FixedRetentionPreview());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.PreviewExpiredAsync(DateTimeOffset.UtcNow, 0));
    }

    private sealed class FixedRetentionPreview(params RetentionCandidate[] candidates) : IRetentionPreview
    {
        public Task<IReadOnlyList<RetentionCandidate>> GetExpiredAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RetentionCandidate>>(candidates.Take(maximumCount).ToList());
    }
}
