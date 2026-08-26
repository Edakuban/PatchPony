using System.Security.Cryptography;
using System.Text;

namespace PatchPony.Core.Knowledge;

/// <summary>Content-free evidence about a knowledge operation. Values that could identify vault content are process-scoped fingerprints.</summary>
public sealed record KnowledgeAuditEvent(
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string ProjectReference,
    string? SessionReference,
    string Operation,
    string Outcome,
    string? PathFingerprint = null,
    string? Revision = null,
    int? ResultCount = null,
    int? AddedLinks = null,
    int? RemovedLinks = null,
    int? IncomingBacklinks = null,
    int? BrokenFragmentBacklinks = null,
    IReadOnlyList<string>? OwnerFingerprints = null,
    IReadOnlyList<string>? ReviewerFingerprints = null);

public interface IKnowledgeAuditSink
{
    void Record(KnowledgeAuditEvent auditEvent);

    IReadOnlyList<KnowledgeAuditEvent> GetRecent(int maximumCount);
}

public sealed class NullKnowledgeAuditSink : IKnowledgeAuditSink
{
    public static readonly NullKnowledgeAuditSink Instance = new();

    private NullKnowledgeAuditSink()
    {
    }

    public void Record(KnowledgeAuditEvent auditEvent)
    {
    }

    public IReadOnlyList<KnowledgeAuditEvent> GetRecent(int maximumCount) => [];
}

public static class KnowledgeAuditFingerprint
{
    private static readonly byte[] ProcessKey = RandomNumberGenerator.GetBytes(32);

    public static string ForValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexString(HMACSHA256.HashData(ProcessKey, Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}