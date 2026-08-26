using PatchPony.Core.Common;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.Infrastructure.Git;

public sealed record SessionDiff(SessionId SessionId, string BranchName, string Content, bool HasChanges);

/// <summary>Returns a bounded, read-only diff for one server-derived session worktree.</summary>
public sealed class GitSessionDiffService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly SessionWorkspaceLayoutResolver layoutResolver;
    private readonly IGitCommandRunner git;

    public GitSessionDiffService(SessionWorkspaceLayoutResolver layoutResolver, IGitCommandRunner? git = null)
    {
        this.layoutResolver = layoutResolver;
        this.git = git ?? new ProcessGitCommandRunner("git", DefaultTimeout, 64 * 1024);
    }

    public async Task<Result<SessionDiff>> GetAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.Status is not (SessionStatus.Active or SessionStatus.Closing))
        {
            return Result<SessionDiff>.Failure(new DomainError("session.diff.invalid_state", "A diff is only available for an active or closing session."));
        }

        var layout = layoutResolver.Resolve(session.Id);
        if (!Directory.Exists(layout.WorktreeRoot))
        {
            return Result<SessionDiff>.Failure(new DomainError("session.worktree_unavailable", "The session worktree is unavailable."));
        }

        if (!SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.WorktreeRoot, requireDirectory: true))
        {
            return Result<SessionDiff>.Failure(new DomainError("session.worktree.unsafe_path", "The controlled session workspace path is unsafe."));
        }

        try
        {
            var command = new GitCommand(null,
            [
                "-C", layout.WorktreeRoot,
                "diff", "--no-ext-diff", "--no-color", "--no-textconv", "--no-renames", "--"
            ]);
            var result = await git.RunAsync(command, cancellationToken);
            if (result.ExitCode != 0)
            {
                return Result<SessionDiff>.Failure(new DomainError("session.diff.failed", "The controlled Git diff failed."));
            }

            var names = SessionNaming.For(session.Id);
            return Result<SessionDiff>.Success(new SessionDiff(session.Id, names.BranchName, result.StandardOutput, !string.IsNullOrEmpty(result.StandardOutput)));
        }
        catch (TimeoutException)
        {
            return Result<SessionDiff>.Failure(new DomainError("session.diff.timeout", "The controlled Git diff timed out."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Result<SessionDiff>.Failure(new DomainError("session.diff.failed", "The controlled Git diff failed."));
        }
    }
}