using PatchPony.Core.Common;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.Infrastructure.Git;

/// <summary>Removes only the worktree and branch derived from a closing session.</summary>
public sealed class GitSessionDiscardService : ISessionWorkspaceDiscarder
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly SessionWorkspaceLayoutResolver layoutResolver;
    private readonly IGitCommandRunner git;
    private readonly SessionWorkspaceLockService locks;

    public GitSessionDiscardService(SessionWorkspaceLayoutResolver layoutResolver, IGitCommandRunner? git = null, SessionWorkspaceLockService? locks = null)
    {
        this.layoutResolver = layoutResolver;
        this.git = git ?? new ProcessGitCommandRunner("git", DefaultTimeout, 16 * 1024);
        this.locks = locks ?? new SessionWorkspaceLockService(layoutResolver);
    }

    public async Task<Result> DiscardAsync(Session session, string baseCheckoutRoot, CancellationToken cancellationToken = default)
    {
        if (session.Status != SessionStatus.Closing)
        {
            return Result.Failure(new DomainError("session.discard.invalid_state", "Only a closing session can be discarded."));
        }

        if (!Path.IsPathFullyQualified(baseCheckoutRoot) || !Directory.Exists(baseCheckoutRoot))
        {
            return Result.Failure(new DomainError("session.base_checkout_unavailable", "The configured base checkout is unavailable."));
        }

        var layout = layoutResolver.Resolve(session.Id);
        if (File.Exists(layout.SessionRoot) || !SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.SessionRoot, requireDirectory: false) ||
            (Directory.Exists(layout.WorktreeRoot) && !SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.WorktreeRoot, requireDirectory: true)))
        {
            return Result.Failure(new DomainError("session.discard.unsafe_path", "The controlled session root is unsafe to discard."));
        }

        var leaseResult = await locks.AcquireAsync(session, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), cancellationToken);
        if (!leaseResult.IsSuccess)
        {
            return Result.Failure(leaseResult.Error);
        }

        var lease = leaseResult.Value!;
        try
        {
            if (Directory.Exists(layout.WorktreeRoot))
            {
                var removed = await RunAsync(
                    ["-C", Path.GetFullPath(baseCheckoutRoot), "worktree", "remove", "--force", "--", layout.WorktreeRoot],
                    cancellationToken);
                if (!removed.IsSuccess)
                {
                    return Result.Failure(removed.Error);
                }
            }

            var branchName = SessionNaming.For(session.Id).BranchName;
            var listed = await RunAsync(
                ["-C", Path.GetFullPath(baseCheckoutRoot), "branch", "--list", "--format=%(refname:short)", "--", branchName],
                cancellationToken);
            if (!listed.IsSuccess)
            {
                return Result.Failure(listed.Error);
            }

            if (listed.Value!.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(branchName, StringComparer.Ordinal))
            {
                var deleted = await RunAsync(
                    ["-C", Path.GetFullPath(baseCheckoutRoot), "branch", "-D", "--", branchName],
                    cancellationToken);
                if (!deleted.IsSuccess)
                {
                    return Result.Failure(deleted.Error);
                }
            }

            if (Directory.Exists(layout.SessionRoot))
            {
                if (!SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.SessionRoot, requireDirectory: true))
                {
                    return Result.Failure(new DomainError("session.discard.unsafe_path", "The controlled session root is unsafe to discard."));
                }

                if (!SessionWorkspacePathGuard.TryDeleteDirectoryTree(layout.StorageRoot, layout.SessionRoot))
                {
                    return Result.Failure(new DomainError("session.discard.cleanup_failed", "The controlled session workspace could not be removed."));
                }
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure(new DomainError("session.discard.cleanup_failed", "The controlled session workspace could not be removed."));
        }
        finally
        {
            await locks.ReleaseAsync(session, lease, CancellationToken.None);
        }
    }

    private async Task<Result<GitCommandResult>> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var result = await git.RunAsync(new GitCommand(null, arguments), cancellationToken);
            return result.ExitCode == 0
                ? Result<GitCommandResult>.Success(result)
                : Result<GitCommandResult>.Failure(new DomainError("session.discard.failed", "The controlled Git discard operation failed."));
        }
        catch (TimeoutException)
        {
            return Result<GitCommandResult>.Failure(new DomainError("session.discard.timeout", "The controlled Git discard operation timed out."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Result<GitCommandResult>.Failure(new DomainError("session.discard.failed", "The controlled Git discard operation failed."));
        }
    }

}