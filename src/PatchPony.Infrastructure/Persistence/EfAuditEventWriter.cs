using PatchPony.Core.Audit;

namespace PatchPony.Infrastructure.Persistence;

public sealed class EfAuditEventWriter(PatchPonyDbContext database) : IAuditEventWriter
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        database.AuditEvents.Add(new AuditEventRow
        {
            Id = auditEvent.Id,
            ProjectId = auditEvent.ProjectId.Value,
            JobId = auditEvent.JobId?.Value,
            EventType = auditEvent.EventType,
            CorrelationId = auditEvent.CorrelationId,
            OccurredAt = auditEvent.OccurredAt,
            MetadataJson = auditEvent.MetadataJson
        });

        await database.SaveChangesAsync(cancellationToken);
    }
}
