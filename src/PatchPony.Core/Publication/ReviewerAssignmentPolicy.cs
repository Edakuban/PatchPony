using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Publication;

public sealed record ProjectReviewerPolicy(ProjectId ProjectId, IReadOnlyList<string> Reviewers);

public sealed class ReviewerAssignmentPolicy
{
    private static readonly Regex ReviewerPattern = new("^[A-Za-z0-9-]{1,39}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private readonly IReadOnlyDictionary<ProjectId, IReadOnlyList<string>> policies;
    public ReviewerAssignmentPolicy(IEnumerable<ProjectReviewerPolicy> policies)
    {
        var mapped = new Dictionary<ProjectId, IReadOnlyList<string>>();
        foreach (var policy in policies)
        {
            var reviewers = policy.Reviewers?.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
            if (policy.ProjectId.Value == Guid.Empty || reviewers.Length == 0 || reviewers.Any(reviewer => !ReviewerPattern.IsMatch(reviewer)) || !mapped.TryAdd(policy.ProjectId, reviewers)) throw new ArgumentException("Reviewer policies require unique projects and valid GitHub usernames.", nameof(policies));
        }
        this.policies = mapped;
    }
    public Result<IReadOnlyList<string>> Resolve(ProjectId projectId) => policies.TryGetValue(projectId, out var reviewers) ? Result<IReadOnlyList<string>>.Success(reviewers) : Result<IReadOnlyList<string>>.Failure(new DomainError("publication.reviewers_missing", "No reviewer policy is configured for this project."));
}

public sealed class GitMergeRequestReviewerService(ReviewerAssignmentPolicy policy)
{
    public async Task<Result> AssignAsync(GitMergeRequestDraft draft, GitMergeRequestReference mergeRequest, IGitHostingProvider provider, CancellationToken cancellationToken = default)
    {
        var reviewers = policy.Resolve(draft.ProjectId);
        if (!reviewers.IsSuccess) return Result.Failure(reviewers.Error);
        return await provider.RequestReviewersAsync(draft, mergeRequest, reviewers.Value!, cancellationToken);
    }
}