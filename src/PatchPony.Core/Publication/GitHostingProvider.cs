using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Publication;

public enum GitHostingProviderKind { GitHub, GitLab }

public sealed record GitMergeRequestDraft(ProjectId ProjectId, JobId JobId, SessionId SessionId, Uri RepositoryUri, string SourceBranch, string TargetBranch, string Title, string Description)
{
    private static readonly Regex BranchPattern = new("^(?!/)(?!.*//)(?!.*(?:^|/)\\.\\.(?:/|$))[A-Za-z0-9._/-]{1,255}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    public Result Validate()
    {
        if (ProjectId.Value == Guid.Empty || JobId.Value == Guid.Empty || SessionId.Value == Guid.Empty || RepositoryUri is null || !RepositoryUri.IsAbsoluteUri || RepositoryUri.Scheme != Uri.UriSchemeHttps || !BranchPattern.IsMatch(SourceBranch ?? string.Empty) || !BranchPattern.IsMatch(TargetBranch ?? string.Empty) || string.Equals(SourceBranch, TargetBranch, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(Title) || Title.Length > 200 || Description is null || Description.Length > 16_000)
            return Result.Failure(DomainError.Validation("A bounded server-owned merge request draft is required."));
        return Result.Success();
    }
}

public sealed record GitMergeRequestReference(GitHostingProviderKind Provider, string ProviderId, Uri Url, string SourceBranch, string TargetBranch);

/// <summary>Provider boundary: credentials and provider HTTP APIs remain behind this server-side contract.</summary>
public interface IGitHostingProvider
{
    GitHostingProviderKind Kind { get; }
    Task<Result<GitMergeRequestReference?>> FindOpenAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default);
    Task<Result<GitMergeRequestReference>> CreateAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default);
    Task<Result> RequestReviewersAsync(GitMergeRequestDraft draft, GitMergeRequestReference mergeRequest, IReadOnlyList<string> reviewers, CancellationToken cancellationToken = default) => Task.FromResult(Result.Failure(new DomainError("git_provider.reviewers_unsupported", "Reviewer assignment is not supported by this provider.")));
}