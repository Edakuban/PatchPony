using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Git;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class GitSessionDiscardServiceTests
{
    [Fact]
    public async Task Discard_RemovesOnlyTheDerivedWorktreeAndBranch()
    {
        var root = CreateTemporaryRoot();
        var baseCheckout = Path.Combine(root, "base-checkout");
        Directory.CreateDirectory(baseCheckout);
        try
        {
            var session = CreateClosingSession();
            var layoutResolver = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            var layout = layoutResolver.Resolve(session.Id);
            Directory.CreateDirectory(layout.WorktreeRoot);
            File.WriteAllText(Path.Combine(layout.WorktreeRoot, "change.txt"), "temporary");
            var git = new RecordingGitCommandRunner($"patchpony/session/{session.Id.Value:N}\n");
            var service = new GitSessionDiscardService(layoutResolver, git);

            var result = await service.DiscardAsync(session, baseCheckout);

            Assert.True(result.IsSuccess);
            Assert.False(Directory.Exists(layout.SessionRoot));
            Assert.Equal(3, git.Commands.Count);
            Assert.Equal(["-C", Path.GetFullPath(baseCheckout), "worktree", "remove", "--force", "--", layout.WorktreeRoot], git.Commands[0].Arguments);
            Assert.Equal(["-C", Path.GetFullPath(baseCheckout), "branch", "--list", "--format=%(refname:short)", "--", $"patchpony/session/{session.Id.Value:N}"], git.Commands[1].Arguments);
            Assert.Equal(["-C", Path.GetFullPath(baseCheckout), "branch", "-D", "--", $"patchpony/session/{session.Id.Value:N}"], git.Commands[2].Arguments);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Discard_RejectsAWorktreeReparsePointWithoutCallingGit()
    {
        var root = CreateTemporaryRoot();
        var baseCheckout = Path.Combine(root, "base-checkout");
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(baseCheckout);
        Directory.CreateDirectory(outside);
        try
        {
            var session = CreateClosingSession();
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            Directory.CreateDirectory(layout.Resolve(session.Id).SessionRoot);
            Directory.CreateSymbolicLink(layout.Resolve(session.Id).WorktreeRoot, outside);
            var git = new RecordingGitCommandRunner(string.Empty);

            var result = await new GitSessionDiscardService(layout, git).DiscardAsync(session, baseCheckout);

            Assert.False(result.IsSuccess);
            Assert.Equal("session.discard.unsafe_path", result.Error.Code);
            Assert.Empty(git.Commands);
            Assert.True(Directory.Exists(outside));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task Discard_RejectsNonClosingSessionsWithoutCallingGit()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var git = new RecordingGitCommandRunner(string.Empty);
            var service = new GitSessionDiscardService(new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage")), git);

            var result = await service.DiscardAsync(Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)).Value!, root);

            Assert.False(result.IsSuccess);
            Assert.Equal("session.discard.invalid_state", result.Error.Code);
            Assert.Empty(git.Commands);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Session CreateClosingSession()
    {
        var now = DateTimeOffset.UtcNow;
        return Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now, now.AddHours(1)).Value!
            .Transition(SessionStatus.Provisioning, now.AddMinutes(1)).Value!
            .Transition(SessionStatus.Active, now.AddMinutes(2)).Value!
            .Transition(SessionStatus.Closing, now.AddMinutes(3)).Value!;
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class RecordingGitCommandRunner(string branchListOutput) : IGitCommandRunner
    {
        public List<GitCommand> Commands { get; } = [];

        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            if (command.Arguments.Contains("worktree"))
            {
                Directory.Delete(command.Arguments[command.Arguments.ToList().IndexOf("--") + 1], recursive: true);
            }

            var output = command.Arguments.Contains("--list") ? branchListOutput : string.Empty;
            return Task.FromResult(new GitCommandResult(0, output, string.Empty));
        }
    }
}