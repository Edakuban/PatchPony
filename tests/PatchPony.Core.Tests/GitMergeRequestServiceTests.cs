using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class GitMergeRequestServiceTests
{
    [Fact]
    public async Task Create_ReusesExistingPullRequestBeforeCallingCreate()
    {
        var (project, job, session, repository) = Fixture();
        var reference = Reference(session);
        var provider = new RecordingProvider(reference);

        var result = await new GitMergeRequestService().CreateAsync(project, job, session, repository, provider);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Created);
        Assert.False(provider.CreateCalled);
    }

    [Fact]
    public async Task Create_RefetchesBeforeRetryingAnUncertainCreate()
    {
        var (project, job, session, repository) = Fixture();
        var provider = new UncertainCreateProvider(Reference(session));

        var result = await new GitMergeRequestService().CreateAsync(project, job, session, repository, provider);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Created);
        Assert.Equal(2, provider.FindCalls);
        Assert.Equal(1, provider.CreateCalls);
    }

    private static (Project Project, Job Job, Session Session, RepositoryRegistration Repository) Fixture()
    {
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(ProjectId.New(), "patchpony", "PatchPony", now).Value!;
        var job = Job.Create(JobId.New(), project.Id, "config.patch", now).Value!;
        var session = Session.Create(SessionId.New(), project.Id, job.Id, now, now.AddHours(1)).Value!;
        var repository = RepositoryRegistration.Create(project.Id, new Uri("https://github.com/Edakuban/PatchPony"), "main").Value!;
        return (project, job, session, repository);
    }

    private static GitMergeRequestReference Reference(Session session) => new(GitHostingProviderKind.GitHub, "42", new Uri("https://github.com/Edakuban/PatchPony/pull/42"), $"patchpony/session/{session.Id.Value:N}", "main");

    private sealed class RecordingProvider(GitMergeRequestReference? existing) : IGitHostingProvider
    {
        public GitHostingProviderKind Kind => GitHostingProviderKind.GitHub;
        public bool CreateCalled { get; private set; }
        public Task<Result<GitMergeRequestReference?>> FindOpenAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default) => Task.FromResult(Result<GitMergeRequestReference?>.Success(existing));
        public Task<Result<GitMergeRequestReference>> CreateAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default) { CreateCalled = true; return Task.FromResult(Result<GitMergeRequestReference>.Success(existing!)); }
    }

    private sealed class UncertainCreateProvider(GitMergeRequestReference reference) : IGitHostingProvider
    {
        public GitHostingProviderKind Kind => GitHostingProviderKind.GitHub;
        public int FindCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public Task<Result<GitMergeRequestReference?>> FindOpenAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default)
        {
            FindCalls++;
            return Task.FromResult(Result<GitMergeRequestReference?>.Success(FindCalls == 1 ? null : reference));
        }
        public Task<Result<GitMergeRequestReference>> CreateAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(Result<GitMergeRequestReference>.Failure(new DomainError("git_provider.request_failed", "response uncertain")));
        }
    }
}