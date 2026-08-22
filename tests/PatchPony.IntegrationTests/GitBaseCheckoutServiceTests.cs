using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Git;

namespace PatchPony.IntegrationTests;

public sealed class GitBaseCheckoutServiceTests
{
    private const string CommitId = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public async Task Ensure_ClonesFetchesChecksOutAndReturnsAnImmutableCommitRevision()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var git = new RecordingGitCommandRunner();
            var project = CreateProject();
            var repository = CreateRepository(project);
            var service = new GitBaseCheckoutService(root, git);

            var result = await service.EnsureAsync(project, repository);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.Created);
            Assert.Equal(CommitId, result.Value.Revision.CommitId);
            Assert.Collection(git.Commands,
                command => Assert.Equal(
                    ["clone", "--no-checkout", "--filter=blob:none", "--", repository.RemoteUri.AbsoluteUri, Path.Combine(root, project.Id.Value.ToString("N"))],
                    command.Arguments),
                command => Assert.Equal(
                    ["fetch", "--no-tags", "--prune", "origin", "+refs/heads/main:refs/remotes/origin/main"],
                    command.Arguments),
                command => Assert.Equal(
                    ["checkout", "--detach", "--force", "refs/remotes/origin/main"],
                    command.Arguments),
                command => Assert.Equal(
                    ["rev-parse", "--verify", "HEAD^{commit}"],
                    command.Arguments));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Ensure_RejectsExistingCheckoutWithAnotherRemoteBeforeFetching()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var project = CreateProject();
            Directory.CreateDirectory(Path.Combine(root, project.Id.Value.ToString("N")));
            var git = new RecordingGitCommandRunner("https://github.com/another/project.git\n");
            var service = new GitBaseCheckoutService(root, git);

            var result = await service.EnsureAsync(project, CreateRepository(project));

            Assert.False(result.IsSuccess);
            Assert.Equal("checkout.remote_mismatch", result.Error.Code);
            Assert.Single(git.Commands);
            Assert.Equal(["remote", "get-url", "origin"], git.Commands[0].Arguments);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Ensure_RejectsARevisionThatIsNotAnImmutableCommitIdentifier()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var project = CreateProject();
            var git = new RecordingGitCommandRunner(revisionOutput: "main\n");
            var service = new GitBaseCheckoutService(root, git);

            var result = await service.EnsureAsync(project, CreateRepository(project));

            Assert.False(result.IsSuccess);
            Assert.Equal("checkout.revision_invalid", result.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Project CreateProject() =>
        Project.Create(ProjectId.New(), "patchpony", "PatchPony", DateTimeOffset.UtcNow).Value!;

    private static RepositoryRegistration CreateRepository(Project project) =>
        RepositoryRegistration.Create(project.Id, new Uri("https://github.com/Edakuban/PatchPony.git"), "main").Value!;

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class RecordingGitCommandRunner(string remoteOutput = "", string revisionOutput = CommitId) : IGitCommandRunner
    {
        public List<GitCommand> Commands { get; } = [];

        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var output = command.Arguments switch
            {
                ["remote", "get-url", "origin"] => remoteOutput,
                ["rev-parse", "--verify", "HEAD^{commit}"] => revisionOutput,
                _ => string.Empty
            };
            return Task.FromResult(new GitCommandResult(0, output, string.Empty));
        }
    }
}