using System.Security.Cryptography;
using System.Text;
using PatchPony.Core.Common;
using PatchPony.Core.Jobs;

namespace PatchPony.Core.Queue;

/// <summary>
/// A signed, worker-bound representation of a leased queue claim. It is intended
/// for the internal hand-off from a dispatcher to the worker that owns the lease.
/// </summary>
public sealed record WorkerClaimProof(
    Guid ClaimId,
    JobId JobId,
    string WorkerId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string Signature);

/// <summary>
/// Signs and verifies internal job claims with a dedicated shared HMAC key.
/// This key is deliberately separate from user, OIDC and service-account secrets.
/// </summary>
public sealed class WorkerClaimAuthenticator
{
    private const int MinimumSigningKeyLength = 32;
    private readonly byte[] signingKey;

    public WorkerClaimAuthenticator(byte[] signingKey)
    {
        ArgumentNullException.ThrowIfNull(signingKey);

        if (signingKey.Length < MinimumSigningKeyLength)
        {
            throw new ArgumentException("A worker claim signing key must contain at least 32 bytes.", nameof(signingKey));
        }

        this.signingKey = signingKey.ToArray();
    }

    public Result<WorkerClaimProof> Issue(JobClaim claim, DateTimeOffset issuedAt)
    {
        if (!IsValidClaim(claim) || issuedAt > claim.ExpiresAt)
        {
            return Result<WorkerClaimProof>.Failure(InvalidClaim());
        }

        var proof = new WorkerClaimProof(
            claim.ClaimId,
            claim.JobId,
            claim.WorkerId,
            issuedAt,
            claim.ExpiresAt,
            Sign(CreatePayload(claim.ClaimId, claim.JobId, claim.WorkerId, issuedAt, claim.ExpiresAt)));

        return Result<WorkerClaimProof>.Success(proof);
    }

    public Result<JobClaim> Verify(WorkerClaimProof proof, string expectedWorkerId, DateTimeOffset now)
    {
        if (!IsValidProof(proof) || !IsValidWorkerId(expectedWorkerId) ||
            !string.Equals(proof.WorkerId, expectedWorkerId, StringComparison.Ordinal))
        {
            return Result<JobClaim>.Failure(InvalidClaim());
        }

        if (proof.ExpiresAt <= now)
        {
            return Result<JobClaim>.Failure(new DomainError("worker_claim.expired", "The worker claim has expired."));
        }

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromBase64String(proof.Signature);
        }
        catch (FormatException)
        {
            return Result<JobClaim>.Failure(InvalidClaim());
        }

        var expectedSignature = ComputeSignature(CreatePayload(
            proof.ClaimId,
            proof.JobId,
            proof.WorkerId,
            proof.IssuedAt,
            proof.ExpiresAt));

        if (!CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
        {
            return Result<JobClaim>.Failure(InvalidClaim());
        }

        return Result<JobClaim>.Success(new JobClaim(proof.ClaimId, proof.JobId, proof.WorkerId, proof.ExpiresAt));
    }

    private string Sign(string payload) => Convert.ToBase64String(ComputeSignature(payload));

    private byte[] ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(signingKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static string CreatePayload(
        Guid claimId,
        JobId jobId,
        string workerId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt) => string.Join(
        '\n',
        "patchpony-worker-claim-v1",
        Convert.ToBase64String(Encoding.UTF8.GetBytes(workerId)),
        claimId.ToString("N"),
        jobId.Value.ToString("N"),
        issuedAt.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
        expiresAt.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static bool IsValidClaim(JobClaim claim) =>
        claim.ClaimId != Guid.Empty &&
        claim.JobId.Value != Guid.Empty &&
        IsValidWorkerId(claim.WorkerId) &&
        claim.ExpiresAt != default;

    private static bool IsValidProof(WorkerClaimProof proof) =>
        proof is not null &&
        proof.ClaimId != Guid.Empty &&
        proof.JobId.Value != Guid.Empty &&
        IsValidWorkerId(proof.WorkerId) &&
        proof.IssuedAt != default &&
        proof.ExpiresAt > proof.IssuedAt &&
        !string.IsNullOrWhiteSpace(proof.Signature);

    private static bool IsValidWorkerId(string? workerId) =>
        !string.IsNullOrWhiteSpace(workerId) && workerId.Length <= 255;

    private static DomainError InvalidClaim() =>
        new("worker_claim.invalid", "The worker claim could not be authenticated.");
}