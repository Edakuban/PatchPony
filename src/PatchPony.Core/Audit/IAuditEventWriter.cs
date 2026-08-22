namespace PatchPony.Core.Audit;

public interface IAuditEventWriter
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
