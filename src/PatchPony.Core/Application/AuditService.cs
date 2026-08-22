using System.Text.Json;
using PatchPony.Core.Audit;
using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Application;

public sealed class AuditService(IProjectRepository projects, IJobRepository jobs, IAuditEventWriter auditEvents, ICorrelationContext correlations)
{
    public async Task<Result<AuditEvent>> AppendAsync(
        ProjectId projectId,
        JobId? jobId,
        string eventType,
        DateTimeOffset occurredAt,
        string metadataJson,
        CancellationToken cancellationToken = default)
    {
        if (projectId.Value == Guid.Empty || string.IsNullOrWhiteSpace(eventType) || eventType.Length > 255 ||
            string.IsNullOrWhiteSpace(metadataJson))
        {
            return Result<AuditEvent>.Failure(DomainError.Validation("Project, event type and JSON metadata are required."));
        }

        try
        {
            using var metadata = JsonDocument.Parse(metadataJson);
        }
        catch (JsonException)
        {
            return Result<AuditEvent>.Failure(new DomainError("audit.metadata.invalid", "Audit metadata must be valid JSON."));
        }

        if (await projects.GetAsync(projectId, cancellationToken) is null)
        {
            return Result<AuditEvent>.Failure(new DomainError("project.not_found", "The project does not exist."));
        }

        if (jobId is { } id)
        {
            var job = await jobs.GetAsync(id, cancellationToken);
            if (job is null || job.ProjectId != projectId)
            {
                return Result<AuditEvent>.Failure(new DomainError("job.not_found", "The job does not belong to the project."));
            }
        }

        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            projectId,
            jobId,
            eventType.Trim(),
            correlations.Current.Value,
            occurredAt,
            metadataJson);
        await auditEvents.AppendAsync(auditEvent, cancellationToken);
        return Result<AuditEvent>.Success(auditEvent);
    }
}
