using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Publication;

public sealed record GitMergeRequestPublication(GitMergeRequestReference Reference, bool Created);

/// <summary>Idempotent provider orchestration for one validated Session branch.</summary>
public sealed class GitMergeRequestService(PublicationRetryPolicy? retry = null)
{
    public async Task<Result<GitMergeRequestPublication>> CreateAsync(Project project, Job job, Session session, RepositoryRegistration repository, IGitHostingProvider provider, CancellationToken cancellationToken = default)
    {
        if (repository is null || provider is null || repository.ProjectId != project.Id || provider.Kind != GitHostingProviderKind.GitHub)
            return Result<GitMergeRequestPublication>.Failure(new DomainError("publication.provider_invalid", "A matching GitHub publication provider and repository are required."));

        var template = GitPublicationTemplate.Create(project, job, session);
        if (!template.IsSuccess) return Result<GitMergeRequestPublication>.Failure(template.Error);

        var draft = new GitMergeRequestDraft(project.Id, job.Id, session.Id, repository.RemoteUri, template.Value!.BranchName, repository.DefaultBranch, $"PatchPony: {project.ManifestId} session {session.Id.Value:N}", $"Automated validated PatchPony session {session.Id.Value:N}.");
        var retries = retry ?? new PublicationRetryPolicy();

        // Each retry observes provider state first. This turns an uncertain create response
        // into a reuse instead of potentially creating a duplicate pull request.
        return await retries.ExecuteAsync(async token =>
        {
            var existing = await provider.FindOpenAsync(draft, token);
            if (!existing.IsSuccess) return Result<GitMergeRequestPublication>.Failure(existing.Error);
            if (existing.Value is not null) return Result<GitMergeRequestPublication>.Success(new GitMergeRequestPublication(existing.Value, false));

            var created = await provider.CreateAsync(draft, token);
            return created.IsSuccess
                ? Result<GitMergeRequestPublication>.Success(new GitMergeRequestPublication(created.Value!, true))
                : Result<GitMergeRequestPublication>.Failure(created.Error);
        }, cancellationToken);
    }
}