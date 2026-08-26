using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class PublicationRetryPolicyTests
{
    [Fact]
    public async Task Execute_RetriesOnlyTransientPublicationFailuresWithStableOperationKey()
    {
        var calls = 0;
        var result = await new PublicationRetryPolicy().ExecuteAsync<int>(_ => Task.FromResult(++calls < 3 ? Result<int>.Failure(new DomainError("git_provider.request_failed", "temporary")) : Result<int>.Success(42)));
        var key = new PublicationOperationKey(ProjectId.New(), JobId.New(), SessionId.New(), PublicationOperation.MergeRequest);
        Assert.True(result.IsSuccess); Assert.Equal(3, calls); Assert.StartsWith("publication:", key.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execute_DoesNotRetryDomainRejections()
    {
        var calls = 0;
        var result = await new PublicationRetryPolicy().ExecuteAsync(_ => Task.FromResult(++calls > 0 ? Result.Failure(new DomainError("publication.approval_required", "approval")) : Result.Success()));
        Assert.False(result.IsSuccess); Assert.Equal(1, calls);
    }

    [Fact]
    public void Constructor_RejectsUnboundedRetryCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PublicationRetryPolicy(4));
    }
}