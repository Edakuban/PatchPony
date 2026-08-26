using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;

namespace PatchPony.Core.Tests;

public sealed class ReviewerAssignmentPolicyTests
{
    [Fact]
    public async Task Assign_UsesOnlyTheConfiguredProjectReviewers()
    {
        var project = ProjectId.New();
        var policy = new ReviewerAssignmentPolicy([new ProjectReviewerPolicy(project, ["alice", "bob"]) ]);
        var draft = new GitMergeRequestDraft(project, PatchPony.Core.Jobs.JobId.New(), PatchPony.Core.Sessions.SessionId.New(), new Uri("https://github.com/Edakuban/PatchPony"), "patchpony/session/x", "main", "Patch", "Description");
        var provider = new RecordingProvider();
        var result = await new GitMergeRequestReviewerService(policy).AssignAsync(draft, new GitMergeRequestReference(GitHostingProviderKind.GitHub, "42", new Uri("https://github.com/Edakuban/PatchPony/pull/42"), draft.SourceBranch, draft.TargetBranch), provider);
        Assert.True(result.IsSuccess);
        Assert.Equal(["alice", "bob"], provider.Reviewers);
    }
    private sealed class RecordingProvider : IGitHostingProvider
    {
        public GitHostingProviderKind Kind => GitHostingProviderKind.GitHub;
        public IReadOnlyList<string> Reviewers { get; private set; } = [];
        public Task<Result<GitMergeRequestReference?>> FindOpenAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Result<GitMergeRequestReference>> CreateAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Result> RequestReviewersAsync(GitMergeRequestDraft draft, GitMergeRequestReference mergeRequest, IReadOnlyList<string> reviewers, CancellationToken cancellationToken = default) { Reviewers = reviewers; return Task.FromResult(Result.Success()); }
    }
}