using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Git;

namespace PatchPony.Infrastructure.Knowledge;

public sealed record KnowledgePublicationResult(KnowledgeSessionPatchResult Patch, GitPublicationCommit Commit, GitMergeRequestPublication MergeRequest);

/// <summary>Human-gated knowledge publication orchestration. It has no merge operation.</summary>
public sealed class KnowledgeSessionPublicationWorkflow(KnowledgeSessionPatchService patches, GitSessionPublicationService publication, PublicationApprovalGate approvals, GitMergeRequestService mergeRequests)
{
    public async Task<Result<KnowledgePublicationResult>> PublishAsync(
        Project project,
        Job job,
        Session session,
        RepositoryRegistration repository,
        KnowledgeSourceProjectAccess access,
        KnowledgeAreaOwnershipPolicy ownership,
        KnowledgePatchRequest request,
        IGitHostingProvider provider,
        CancellationToken cancellationToken = default)
    {
        if (project is null || job is null || repository is null || provider is null || project.Id != session.ProjectId || job.ProjectId != project.Id || repository.ProjectId != project.Id)
            return Result<KnowledgePublicationResult>.Failure(new DomainError("knowledge.publication.invalid", "A matching project, job, session, repository and provider are required."));

        // Approval is checked before touching the worktree. The same job therefore needs an
        // explicit human decision before either external publication step can begin.
        var pushApproval = await approvals.EnsureApprovedAsync(project.Id, job.Id, PublicationAction.Push, cancellationToken);
        if (!pushApproval.IsSuccess) return Result<KnowledgePublicationResult>.Failure(pushApproval.Error);
        var mergeApproval = await approvals.EnsureApprovedAsync(project.Id, job.Id, PublicationAction.MergeRequest, cancellationToken);
        if (!mergeApproval.IsSuccess) return Result<KnowledgePublicationResult>.Failure(mergeApproval.Error);

        var patch = patches.Apply(session, access, ownership, request, cancellationToken);
        if (!patch.IsSuccess) return Result<KnowledgePublicationResult>.Failure(patch.Error);
        var template = GitPublicationTemplate.Create(project, job, session);
        if (!template.IsSuccess) return Result<KnowledgePublicationResult>.Failure(template.Error);
        var templateValue = template.Value!;
        var patchValue = patch.Value!;
        var commit = await publication.CommitAsync(session, templateValue, cancellationToken);
        if (!commit.IsSuccess) return Result<KnowledgePublicationResult>.Failure(commit.Error);
        var pushed = await publication.PushAsync(session, repository, templateValue, cancellationToken);
        if (!pushed.IsSuccess) return Result<KnowledgePublicationResult>.Failure(pushed.Error);
        var mergeRequest = await mergeRequests.CreateAsync(project, job, session, repository, provider, cancellationToken);
        if (!mergeRequest.IsSuccess) return Result<KnowledgePublicationResult>.Failure(mergeRequest.Error);
        var reviewers = await provider.RequestReviewersAsync(
            new GitMergeRequestDraft(project.Id, job.Id, session.Id, repository.RemoteUri, templateValue.BranchName, repository.DefaultBranch, $"PatchPony knowledge session {session.Id.Value:N}", "Validated knowledge change; human merge required."),
            mergeRequest.Value!.Reference,
            patchValue.Ownership.Reviewers,
            cancellationToken);
        if (!reviewers.IsSuccess) return Result<KnowledgePublicationResult>.Failure(reviewers.Error);
        return Result<KnowledgePublicationResult>.Success(new KnowledgePublicationResult(patchValue, commit.Value!, mergeRequest.Value));
    }
}
