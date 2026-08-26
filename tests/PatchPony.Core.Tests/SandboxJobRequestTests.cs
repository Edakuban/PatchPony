using PatchPony.Core.Sandbox;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class SandboxJobRequestTests
{
    [Fact]
    public void ValidateContract_RequiresOnlyAWorkerClaimSessionAndRegisteredTestId()
    {
        var request = new SandboxJobRequest(
            new PatchPony.Core.Queue.WorkerClaimProof(Guid.NewGuid(), PatchPony.Core.Jobs.JobId.New(), "worker-a", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1), "signature"),
            SessionId.New(),
            "test.billing.unit");
        var imageAttempt = request with { CommandId = "docker run alpine" };

        Assert.True(request.ValidateContract().IsSuccess);
        Assert.False(imageAttempt.ValidateContract().IsSuccess);
        Assert.Equal("validation.invalid", imageAttempt.ValidateContract().Error.Code);
    }
}