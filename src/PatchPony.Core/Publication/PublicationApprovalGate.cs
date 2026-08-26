using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Workflows;

namespace PatchPony.Core.Publication;

public enum PublicationAction { Push, MergeRequest }
public sealed record ProjectPublicationApprovalPolicy(ProjectId ProjectId, bool RequirePushApproval, bool RequireMergeRequestApproval);

public interface IApprovalRepository
{
    Task<Approval?> GetLatestAsync(JobId jobId, CancellationToken cancellationToken = default);
}

public sealed class PublicationApprovalGate(IEnumerable<ProjectPublicationApprovalPolicy> policies, IApprovalRepository approvals)
{
    private readonly IReadOnlyDictionary<ProjectId, ProjectPublicationApprovalPolicy> policies = policies.ToDictionary(policy => policy.ProjectId);

    public async Task<Result> EnsureApprovedAsync(ProjectId projectId, JobId jobId, PublicationAction action, CancellationToken cancellationToken = default)
    {
        if (!this.policies.TryGetValue(projectId, out var policy)) return Result.Failure(new DomainError("publication.approval_policy_missing", "No publication approval policy is configured for this project."));
        var required = action == PublicationAction.Push ? policy.RequirePushApproval : policy.RequireMergeRequestApproval;
        if (!required) return Result.Success();
        var approval = await approvals.GetLatestAsync(jobId, cancellationToken);
        return approval?.Decision == ApprovalDecision.Approved ? Result.Success() : Result.Failure(new DomainError("publication.approval_required", "A human approval is required before publication."));
    }
}