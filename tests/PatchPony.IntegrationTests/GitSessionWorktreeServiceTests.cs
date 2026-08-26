using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Git;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class GitSessionWorktreeServiceTests
{
    [Fact]
    public async Task Create_UsesOnlyAProvisioningSessionAndFixedGitArguments()
    {
        var root = CreateTemporaryRoot();
        var baseCheckout = Path.Combine(root, "base-checkout");
        Directory.CreateDirectory(baseCheckout);
        try
        {
            var session = CreateSession(root).Transition(SessionStatus.Provisioning, DateTimeOffset.UtcNow.AddMinutes(1)).Value!;
            var git = new RecordingGitCommandRunner();
            var service = new GitSessionWorktreeService(new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage")), git);

            var result = await service.CreateAsync(session, baseCheckout);

            Assert.True(result.IsSuccess);
            Assert.Equal($"patchpony/session/{session.Id.Value:N}", result.Value!.BranchName);
            var command = Assert.Single(git.Commands);
            Assert.Equal("-c", command.Arguments[0]);
            Assert.StartsWith("core.hooksPath=", command.Arguments[1], StringComparison.Ordinal);
            Assert.Equal(["-C", Path.GetFullPath(baseCheckout), "worktree", "add", "-b", result.Value.BranchName, "--", result.Value.WorktreePath, "HEAD"], command.Arguments.Skip(2));
            Assert.True(Directory.Exists(Path.GetDirectoryName(result.Value.WorktreePath)!));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Create_RejectsASessionRootReparsePointWithoutCallingGit()
    {
        var root = CreateTemporaryRoot();
        var baseCheckout = Path.Combine(root, "base-checkout");
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(baseCheckout);
        Directory.CreateDirectory(outside);
        try
        {
            var session = CreateSession(root).Transition(SessionStatus.Provisioning, DateTimeOffset.UtcNow.AddMinutes(1)).Value!;
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            Directory.CreateDirectory(Path.GetDirectoryName(layout.Resolve(session.Id).SessionRoot)!);
            Directory.CreateSymbolicLink(layout.Resolve(session.Id).SessionRoot, outside);
            var git = new RecordingGitCommandRunner();

            var result = await new GitSessionWorktreeService(layout, git).CreateAsync(session, baseCheckout);

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
    public async Task Create_RejectsAWorktreeThatExceedsItsServerSideSizeLimit()
    {
        var root = CreateTemporaryRoot();
        var baseCheckout = Path.Combine(root, "base-checkout");
        Directory.CreateDirectory(baseCheckout);
        try
        {
            var session = CreateSession(root).Transition(SessionStatus.Provisioning, DateTimeOffset.UtcNow.AddMinutes(1)).Value!;
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            var service = new GitSessionWorktreeService(layout, new SizedWorktreeGitCommandRunner(3), workspaceSizeLimiter: new SessionWorkspaceSizeLimiter(2));

            var result = await service.CreateAsync(session, baseCheckout);

            Assert.False(result.IsSuccess);
            Assert.Equal("session.workspace_size_exceeded", result.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task Create_RejectsNonProvisioningAndExistingSessionWorkspaces()
    {
        var root = CreateTemporaryRoot();
        var baseCheckout = Path.Combine(root, "base-checkout");
        Directory.CreateDirectory(baseCheckout);
        try
        {
            var session = CreateSession(root);
            var layout = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
            var git = new RecordingGitCommandRunner();
            var service = new GitSessionWorktreeService(layout, git);

            var wrongState = await service.CreateAsync(session, baseCheckout);
            Directory.CreateDirectory(layout.Resolve(session.Id).SessionRoot);
            var provisioning = session.Transition(SessionStatus.Provisioning, DateTimeOffset.UtcNow.AddMinutes(1)).Value!;
            var existing = await service.CreateAsync(provisioning, baseCheckout);

            Assert.False(wrongState.IsSuccess);
            Assert.Equal("session.worktree.invalid_state", wrongState.Error.Code);
            Assert.False(existing.IsSuccess);
            Assert.Equal("session.worktree_exists", existing.Error.Code);
            Assert.Empty(git.Commands);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Session CreateSession(string root)
    {
        var now = DateTimeOffset.UtcNow;
        return Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now, now.AddHours(1)).Value!;
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class RecordingGitCommandRunner : IGitCommandRunner
    {
        public List<GitCommand> Commands { get; } = [];

        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            Directory.CreateDirectory(command.Arguments[command.Arguments.ToList().IndexOf("--") + 1]);
            return Task.FromResult(new GitCommandResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class SizedWorktreeGitCommandRunner(int bytes) : IGitCommandRunner
    {
        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            var worktree = command.Arguments[command.Arguments.ToList().IndexOf("--") + 1];
            Directory.CreateDirectory(worktree);
            File.WriteAllBytes(Path.Combine(worktree, "generated.bin"), new byte[bytes]);
            return Task.FromResult(new GitCommandResult(0, string.Empty, string.Empty));
        }
    }
}