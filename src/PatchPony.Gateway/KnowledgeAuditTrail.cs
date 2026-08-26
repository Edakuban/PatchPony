using System.Collections.Concurrent;
using PatchPony.Core.Knowledge;

namespace PatchPony.Gateway;

/// <summary>Bounded reviewer-visible Knowledge audit trail. It never accepts raw vault content, paths, queries or owner names.</summary>
public sealed class KnowledgeAuditTrail(ILogger<KnowledgeAuditTrail> logger) : IKnowledgeAuditSink
{
    private const int MaximumRetainedEvents = 1_000;
    private readonly ConcurrentQueue<KnowledgeAuditEvent> events = new();

    public void Record(KnowledgeAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        events.Enqueue(auditEvent);
        while (events.Count > MaximumRetainedEvents && events.TryDequeue(out _))
        {
        }

        logger.LogInformation(
            "Knowledge audit {Operation} {Outcome}; project {ProjectReference}; session {SessionReference}; path fingerprint {PathFingerprint}; revision {Revision}; correlation {CorrelationId}",
            auditEvent.Operation,
            auditEvent.Outcome,
            auditEvent.ProjectReference,
            auditEvent.SessionReference,
            auditEvent.PathFingerprint,
            auditEvent.Revision,
            auditEvent.CorrelationId);
    }

    public IReadOnlyList<KnowledgeAuditEvent> GetRecent(int maximumCount) =>
        events.Reverse().Take(Math.Clamp(maximumCount, 1, MaximumRetainedEvents)).ToArray();
}