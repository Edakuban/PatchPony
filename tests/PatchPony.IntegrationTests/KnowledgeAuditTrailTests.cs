using Microsoft.Extensions.Logging.Abstractions;
using PatchPony.Core.Knowledge;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeAuditTrailTests
{
    [Fact]
    public void Trail_RetainsOnlyContentFreeFingerprintMetadata()
    {
        const string path = "runbooks/payment-recovery.md";
        const string owner = "payment-owner";
        var audit = new KnowledgeAuditTrail(NullLogger<KnowledgeAuditTrail>.Instance);
        var pathFingerprint = KnowledgeAuditFingerprint.ForValue(path);
        var ownerFingerprint = KnowledgeAuditFingerprint.ForValue(owner);

        audit.Record(new KnowledgeAuditEvent(
            DateTimeOffset.UtcNow,
            "correlation-42",
            "project-a",
            "session-42",
            "read",
            "completed",
            pathFingerprint,
            "0123456789abcdef0123456789abcdef01234567",
            12,
            OwnerFingerprints: [ownerFingerprint]));

        var stored = Assert.Single(audit.GetRecent(100));
        Assert.Equal(pathFingerprint, stored.PathFingerprint);
        Assert.Equal(ownerFingerprint, Assert.Single(stored.OwnerFingerprints!));
        Assert.DoesNotContain(path, stored.PathFingerprint!, StringComparison.Ordinal);
        Assert.DoesNotContain(owner, stored.OwnerFingerprints![0], StringComparison.Ordinal);
        Assert.Matches("^[0-9a-f]{64}$", stored.PathFingerprint!);
    }
}