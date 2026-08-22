using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Audit;

public sealed record AuditEvent(
    Guid Id,
    ProjectId ProjectId,
    JobId? JobId,
    string EventType,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string MetadataJson);
