using PatchPony.Core.Jobs;
using PatchPony.Core.Queue;

namespace PatchPony.Core.Tests;

public sealed class WorkerClaimAuthenticatorTests
{
    private static readonly byte[] SigningKey = Convert.FromHexString("6D8F740C4050F2A19B9C5EAD3F847D2AB01C899E1256D7B8A308C5F6D4EA197B");

    [Fact]
    public void IssueAndVerify_ReturnsTheBoundLeaseForItsWorker()
    {
        var authenticator = new WorkerClaimAuthenticator(SigningKey);
        var now = DateTimeOffset.UtcNow;
        var claim = new JobClaim(Guid.NewGuid(), JobId.New(), "worker-a", now.AddMinutes(2));

        var proof = authenticator.Issue(claim, now);
        var verification = authenticator.Verify(proof.Value!, "worker-a", now.AddSeconds(1));

        Assert.True(proof.IsSuccess);
        Assert.True(verification.IsSuccess);
        Assert.Equal(claim, verification.Value);
    }

    [Fact]
    public void Verify_RejectsTamperedWorkerJobAndSignature()
    {
        var authenticator = new WorkerClaimAuthenticator(SigningKey);
        var now = DateTimeOffset.UtcNow;
        var proof = authenticator.Issue(new JobClaim(Guid.NewGuid(), JobId.New(), "worker-a", now.AddMinutes(2)), now).Value!;

        var wrongWorker = authenticator.Verify(proof, "worker-b", now);
        var wrongJob = authenticator.Verify(proof with { JobId = JobId.New() }, "worker-a", now);
        var wrongSignature = authenticator.Verify(proof with { Signature = Convert.ToBase64String(new byte[32]) }, "worker-a", now);

        Assert.Equal("worker_claim.invalid", wrongWorker.Error.Code);
        Assert.Equal("worker_claim.invalid", wrongJob.Error.Code);
        Assert.Equal("worker_claim.invalid", wrongSignature.Error.Code);
    }

    [Fact]
    public void Verify_RejectsExpiredProof()
    {
        var authenticator = new WorkerClaimAuthenticator(SigningKey);
        var now = DateTimeOffset.UtcNow;
        var proof = authenticator.Issue(new JobClaim(Guid.NewGuid(), JobId.New(), "worker-a", now.AddMinutes(1)), now).Value!;

        var result = authenticator.Verify(proof, "worker-a", now.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("worker_claim.expired", result.Error.Code);
    }

    [Fact]
    public void Constructor_RejectsShortSigningKeys()
    {
        Assert.Throws<ArgumentException>(() => new WorkerClaimAuthenticator(new byte[31]));
    }
}