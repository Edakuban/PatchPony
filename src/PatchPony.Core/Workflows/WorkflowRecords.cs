using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Workflows;

public enum ApprovalDecision
{
    Pending,
    Approved,
    Rejected
}

public sealed record Approval(Guid Id, JobId JobId, ApprovalDecision Decision, string RequestedBy, DateTimeOffset RequestedAt);

public sealed record ArtifactReference(Guid Id, JobId JobId, string Kind, Uri Location, string ContentHash, DateTimeOffset CreatedAt);

public sealed record ToolInvocation(Guid Id, JobId JobId, string ToolName, string RedactedParametersJson, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);

public sealed record IdempotencyRecord(string Key, ProjectId ProjectId, JobId JobId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
