using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Git;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class GitSessionDiffServiceTests
{
    [Fact]
    public async Task Get_UsesOnlyTheServerDerivedWorktreeAndFixedDiffArguments()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var session = CreateActiveSession();
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            Directory.CreateDirectory(layout.Resolve(session.Id).WorktreeRoot);
            var git = new RecordingGitCommandRunner("diff --git a/example.txt b/example.txt\n");
            var service = new GitSessionDiffService(layout, git);

            var result = await service.GetAsync(session);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.HasChanges);
            Assert.Equal($"patchpony/session/{session.Id.Value:N}", result.Value.BranchName);
            var command = Assert.Single(git.Commands);
            Assert.Equal(["-C", layout.Resolve(session.Id).WorktreeRoot, "diff", "--no-ext-diff", "--no-color", "--no-textconv", "--no-renames", "--"], command.Arguments);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Get_RejectsAWorktreeReparsePointWithoutCallingGit()
    {
        var root = CreateTemporaryRoot();
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        try
        {
            var session = CreateActiveSession();
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            Directory.CreateDirectory(layout.Resolve(session.Id).SessionRoot);
            Directory.CreateSymbolicLink(layout.Resolve(session.Id).WorktreeRoot, outside);
            var git = new RecordingGitCommandRunner(string.Empty);

            var result = await new GitSessionDiffService(layout, git).GetAsync(session);

            Assert.False(result.IsSuccess);
            Assert.Equal("session.worktree.unsafe_path", result.Error.Code);
            Assert.Empty(git.Commands);
            Assert.True(Directory.Exists(outside));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task Get_RejectsNonActiveAndMissingWorktrees()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            var git = new RecordingGitCommandRunner(string.Empty);
            var service = new GitSessionDiffService(layout, git);
            var created = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)).Value!;
            var active = created.Transition(SessionStatus.Provisioning, DateTimeOffset.UtcNow.AddMinutes(1)).Value!
                .Transition(SessionStatus.Active, DateTimeOffset.UtcNow.AddMinutes(2)).Value!;

            var invalidState = await service.GetAsync(created);
            var missingWorktree = await service.GetAsync(active);

            Assert.False(invalidState.IsSuccess);
            Assert.Equal("session.diff.invalid_state", invalidState.Error.Code);
            Assert.False(missingWorktree.IsSuccess);
            Assert.Equal("session.worktree_unavailable", missingWorktree.Error.Code);
            Assert.Empty(git.Commands);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Session CreateActiveSession()
    {
        var now = DateTimeOffset.UtcNow;
        return Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now, now.AddHours(1)).Value!
            .Transition(SessionStatus.Provisioning, now.AddMinutes(1)).Value!
            .Transition(SessionStatus.Active, now.AddMinutes(2)).Value!;
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class RecordingGitCommandRunner(string output) : IGitCommandRunner
    {
        public List<GitCommand> Commands { get; } = [];

        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromResult(new GitCommandResult(0, output, string.Empty));
        }
    }
}