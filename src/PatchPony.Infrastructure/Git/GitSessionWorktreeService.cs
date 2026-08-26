using PatchPony.Core.Common;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.Infrastructure.Git;

public sealed record SessionWorktree(SessionId SessionId, string BranchName, string WorktreePath);

public sealed class GitSessionWorktreeService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly SessionWorkspaceLayoutResolver layoutResolver;
    private readonly IGitCommandRunner git;
    private readonly SessionWorkspaceLockService locks;
    private readonly SessionWorkspaceSizeLimiter workspaceSizeLimiter;

    public GitSessionWorktreeService(SessionWorkspaceLayoutResolver layoutResolver, IGitCommandRunner? git = null, SessionWorkspaceLockService? locks = null, SessionWorkspaceSizeLimiter? workspaceSizeLimiter = null)
    {
        this.layoutResolver = layoutResolver;
        this.git = git ?? new ProcessGitCommandRunner("git", DefaultTimeout, 16 * 1024);
        this.locks = locks ?? new SessionWorkspaceLockService(layoutResolver);
        this.workspaceSizeLimiter = workspaceSizeLimiter ?? new SessionWorkspaceSizeLimiter();
    }

    public async Task<Result<SessionWorktree>> CreateAsync(Session session, string baseCheckoutRoot, CancellationToken cancellationToken = default)
    {
        if (session.Status != SessionStatus.Provisioning)
        {
            return Result<SessionWorktree>.Failure(new DomainError("session.worktree.invalid_state", "A worktree can only be created for a provisioning session."));
        }

        if (!Path.IsPathFullyQualified(baseCheckoutRoot) || !Directory.Exists(baseCheckoutRoot))
        {
            return Result<SessionWorktree>.Failure(new DomainError("session.base_checkout_unavailable", "The configured base checkout is unavailable."));
        }

        var names = SessionNaming.For(session.Id);
        var layout = layoutResolver.Resolve(session.Id);
        if (!SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.SessionRoot, requireDirectory: false))
        {
            return Result<SessionWorktree>.Failure(new DomainError("session.worktree.unsafe_path", "The controlled session workspace path is unsafe."));
        }

        var leaseResult = await locks.AcquireAsync(session, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), cancellationToken);
        if (!leaseResult.IsSuccess)
        {
            return Result<SessionWorktree>.Failure(leaseResult.Error);
        }

        var lease = leaseResult.Value!;
        try
        {
            if (Directory.Exists(layout.SessionRoot) || File.Exists(layout.SessionRoot))
            {
                return Result<SessionWorktree>.Failure(new DomainError("session.worktree_exists", "The session workspace already exists."));
            }

            var branchName = names.BranchName;
            Directory.CreateDirectory(layout.SessionRoot);
            Directory.CreateDirectory(layout.DisabledHooksPath);
            if (!SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.DisabledHooksPath, requireDirectory: true) ||
                !SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.WorktreeRoot, requireDirectory: false))
            {
                return Result<SessionWorktree>.Failure(new DomainError("session.worktree.unsafe_path", "The controlled session workspace path is unsafe."));
            }

            var command = new GitCommand(null,
            [
                "-c", $"core.hooksPath={layout.DisabledHooksPath}",
                "-C", Path.GetFullPath(baseCheckoutRoot),
                "worktree", "add", "-b", branchName, "--", layout.WorktreeRoot, "HEAD"
            ]);
            var result = await git.RunAsync(command, cancellationToken);
            if (result.ExitCode != 0)
            {
                return Result<SessionWorktree>.Failure(new DomainError("session.worktree_failed", "The controlled worktree creation failed."));
            }

            var size = workspaceSizeLimiter.Measure(layout.StorageRoot, layout.WorktreeRoot);
            if (!size.IsSuccess)
            {
                return Result<SessionWorktree>.Failure(size.Error);
            }

            if (size.Value!.ExceedsLimit)
            {
                return Result<SessionWorktree>.Failure(new DomainError("session.workspace_size_exceeded", "The session worktree exceeds the configured size limit."));
            }

            return Result<SessionWorktree>.Success(new SessionWorktree(session.Id, branchName, layout.WorktreeRoot));
        }
        catch (TimeoutException)
        {
            return Result<SessionWorktree>.Failure(new DomainError("session.worktree_timeout", "The controlled worktree creation timed out."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<SessionWorktree>.Failure(new DomainError("session.worktree_failed", "The controlled worktree creation failed."));
        }
        finally
        {
            await locks.ReleaseAsync(session, lease, CancellationToken.None);
        }
    }
}