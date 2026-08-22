using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Git;

namespace PatchPony.IntegrationTests;

public sealed class GitKnowledgeSourceCheckoutServiceTests
{
    private const string CommitId = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public async Task Ensure_ClonesTheVaultAndReturnsAnImmutableCommitRevision()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = KnowledgeSource.Create(KnowledgeSourceId.New(), ProjectId.New(), "vault", new Uri("https://github.com/example/vault.git"), "main", DateTimeOffset.UtcNow).Value!;
            var git = new RecordingGitCommandRunner();

            var result = await new GitKnowledgeSourceCheckoutService(root, git).EnsureAsync(source);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.Created);
            Assert.Equal(source.Id, result.Value.SourceId);
            Assert.Equal(CommitId, result.Value.Revision.CommitId);
            Assert.Collection(git.Commands,
                command => Assert.Equal(["clone", "--no-checkout", "--filter=blob:none", "--", source.RemoteUri.AbsoluteUri, Path.Combine(root, source.Id.Value.ToString("N"))], command.Arguments),
                command => Assert.Equal(["fetch", "--no-tags", "--prune", "origin", "+refs/heads/main:refs/remotes/origin/main"], command.Arguments),
                command => Assert.Equal(["checkout", "--detach", "--force", "refs/remotes/origin/main"], command.Arguments),
                command => Assert.Equal(["rev-parse", "--verify", "HEAD^{commit}"], command.Arguments));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Ensure_RejectsExistingCheckoutWithAnotherRemoteBeforeFetching()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = KnowledgeSource.Create(KnowledgeSourceId.New(), ProjectId.New(), "vault", new Uri("https://github.com/example/vault.git"), "main", DateTimeOffset.UtcNow).Value!;
            Directory.CreateDirectory(Path.Combine(root, source.Id.Value.ToString("N")));
            var git = new RecordingGitCommandRunner("https://github.com/another/vault.git\n");

            var result = await new GitKnowledgeSourceCheckoutService(root, git).EnsureAsync(source);

            Assert.False(result.IsSuccess);
            Assert.Equal("checkout.remote_mismatch", result.Error.Code);
            Assert.Single(git.Commands);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RecordingGitCommandRunner(string remoteOutput = "") : IGitCommandRunner
    {
        public List<GitCommand> Commands { get; } = [];

        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var output = command.Arguments switch
            {
                ["remote", "get-url", "origin"] => remoteOutput,
                ["rev-parse", "--verify", "HEAD^{commit}"] => CommitId,
                _ => string.Empty
            };
            return Task.FromResult(new GitCommandResult(0, output, string.Empty));
        }
    }
}
