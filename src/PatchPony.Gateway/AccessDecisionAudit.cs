using System.Collections.Concurrent;
using System.Security.Claims;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record AccessDecisionAuditEvent(
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string Category,
    string Outcome,
    string Subject,
    string AuthenticationMode,
    string Method,
    string Path,
    string? ProjectId,
    string? Tool,
    string? RequiredScope);

public interface IAccessDecisionAudit
{
    void Record(AccessDecisionAuditEvent auditEvent);

    IReadOnlyList<AccessDecisionAuditEvent> GetRecent(int maximumCount);
}

public sealed class AccessDecisionAudit(ILogger<AccessDecisionAudit> logger, IGatewayLogRedactor redactor) : IAccessDecisionAudit
{
    private const int MaximumRetainedEvents = 1_000;
    private readonly ConcurrentQueue<AccessDecisionAuditEvent> events = new();

    public void Record(AccessDecisionAuditEvent auditEvent)
    {
        var safeEvent = redactor.Redact(auditEvent);
        events.Enqueue(safeEvent);
        while (events.Count > MaximumRetainedEvents && events.TryDequeue(out _))
        {
        }

        logger.LogInformation(
            "Access decision audit {Category} {Outcome} for {Subject} via {AuthenticationMode} on {Method} {Path}; project {ProjectId}; tool {Tool}; scope {RequiredScope}; correlation {CorrelationId}",
            safeEvent.Category,
            safeEvent.Outcome,
            safeEvent.Subject,
            safeEvent.AuthenticationMode,
            safeEvent.Method,
            safeEvent.Path,
            safeEvent.ProjectId,
            safeEvent.Tool,
            safeEvent.RequiredScope,
            auditEvent.CorrelationId);
    }

    public IReadOnlyList<AccessDecisionAuditEvent> GetRecent(int maximumCount) =>
        events.Reverse().Take(Math.Clamp(maximumCount, 1, MaximumRetainedEvents)).ToArray();

    public static string Subject(ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

    public static string AuthenticationMode(ClaimsPrincipal user) =>
        user.FindFirst("auth_mode")?.Value ?? user.Identity?.AuthenticationType ?? "none";
}