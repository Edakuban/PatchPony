using PatchPony.Core.Common;
using PatchPony.Core.Publication;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.Infrastructure.Git;

public sealed record GitPublicationStatus(string BranchName, bool HasChanges, string Porcelain);
public sealed record GitPublicationCommit(string BranchName, string CommitMessage);

/// <summary>Controlled status, commit and push operations for an already provisioned session worktree.</summary>
public sealed class GitSessionPublicationService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly SessionWorkspaceLayoutResolver layouts;
    private readonly IGitCommandRunner git;
    private readonly PublicationRetryPolicy retry;

    public GitSessionPublicationService(SessionWorkspaceLayoutResolver layouts, IGitCommandRunner? git = null, PublicationRetryPolicy? retry = null)
    {
        this.layouts = layouts;
        this.git = git ?? new ProcessGitCommandRunner("git", DefaultTimeout, 16 * 1024);
        this.retry = retry ?? new PublicationRetryPolicy();
    }

    public async Task<Result<GitPublicationStatus>> StatusAsync(Session session, GitPublicationTemplate template, CancellationToken cancellationToken = default)
    {
        var ready = Validate(session, template);
        if (!ready.IsSuccess) return Result<GitPublicationStatus>.Failure(ready.Error);
        var layout = layouts.Resolve(session.Id);
        try
        {
            var branch = await git.RunAsync(new GitCommand(null, ["-C", layout.WorktreeRoot, "branch", "--show-current"]), cancellationToken);
            if (branch.ExitCode != 0 || !string.Equals(branch.StandardOutput.Trim(), template.BranchName, StringComparison.Ordinal)) return Result<GitPublicationStatus>.Failure(new DomainError("publication.branch_mismatch", "The controlled worktree is not on the expected session branch."));
            var status = await git.RunAsync(new GitCommand(null, ["-C", layout.WorktreeRoot, "status", "--porcelain=v1", "-z", "--untracked-files=all"]), cancellationToken);
            return status.ExitCode == 0 ? Result<GitPublicationStatus>.Success(new GitPublicationStatus(template.BranchName, !string.IsNullOrEmpty(status.StandardOutput), status.StandardOutput)) : Result<GitPublicationStatus>.Failure(new DomainError("publication.status_failed", "The controlled Git status failed."));
        }
        catch (TimeoutException) { return Result<GitPublicationStatus>.Failure(new DomainError("publication.status_timeout", "The controlled Git status timed out.")); }
    }

    public async Task<Result<GitPublicationCommit>> CommitAsync(Session session, GitPublicationTemplate template, CancellationToken cancellationToken = default)
    {
        var status = await StatusAsync(session, template, cancellationToken);
        if (!status.IsSuccess) return Result<GitPublicationCommit>.Failure(status.Error);
        if (!status.Value!.HasChanges) return Result<GitPublicationCommit>.Failure(new DomainError("publication.no_changes", "There are no controlled changes to commit."));
        var layout = layouts.Resolve(session.Id);
        try
        {
            var add = await git.RunAsync(new GitCommand(null, ["-C", layout.WorktreeRoot, "add", "--all"]), cancellationToken);
            if (add.ExitCode != 0) return Result<GitPublicationCommit>.Failure(new DomainError("publication.commit_failed", "The controlled Git commit failed."));

            // The commit subject/body is deterministic. A retry first verifies whether a
            // prior uncertain attempt already committed that exact server-derived message.
            return await retry.ExecuteAsync(async token =>
            {
                var head = await git.RunAsync(new GitCommand(null, ["-C", layout.WorktreeRoot, "log", "-1", "--format=%B"]), token);
                if (head.ExitCode == 0 && string.Equals(head.StandardOutput.TrimEnd(), template.CommitMessage, StringComparison.Ordinal))
                    return Result<GitPublicationCommit>.Success(new GitPublicationCommit(template.BranchName, template.CommitMessage));

                var commit = await git.RunAsync(new GitCommand(null, ["-c", $"core.hooksPath={layout.DisabledHooksPath}", "-C", layout.WorktreeRoot, "commit", "--no-verify", "-m", template.CommitMessage]), token);
                return commit.ExitCode == 0
                    ? Result<GitPublicationCommit>.Success(new GitPublicationCommit(template.BranchName, template.CommitMessage))
                    : Result<GitPublicationCommit>.Failure(new DomainError("publication.commit_failed", "The controlled Git commit failed."));
            }, cancellationToken);
        }
        catch (TimeoutException) { return Result<GitPublicationCommit>.Failure(new DomainError("publication.commit_timeout", "The controlled Git commit timed out.")); }
    }

    public async Task<Result> PushAsync(Session session, RepositoryRegistration repository, GitPublicationTemplate template, CancellationToken cancellationToken = default)
    {
        var ready = Validate(session, template);
        if (!ready.IsSuccess) return ready;
        if (repository is null || repository.ProjectId != session.ProjectId || repository.RemoteUri.Scheme != Uri.UriSchemeHttps) return Result.Failure(new DomainError("publication.remote_invalid", "A matching registered HTTPS repository is required."));
        var layout = layouts.Resolve(session.Id);
        try
        {
            var remote = await git.RunAsync(new GitCommand(null, ["-C", layout.WorktreeRoot, "remote", "get-url", "origin"]), cancellationToken);
            if (remote.ExitCode != 0 || !Uri.TryCreate(remote.StandardOutput.Trim(), UriKind.Absolute, out var actual) || !SameRemote(actual, repository.RemoteUri))
                return Result.Failure(new DomainError("publication.remote_mismatch", "The worktree origin does not match the registered repository."));

            var expectedRef = $"refs/heads/{template.BranchName}";
            return await retry.ExecuteAsync(async token =>
            {
                var push = await git.RunAsync(new GitCommand(null, ["-c", $"core.hooksPath={layout.DisabledHooksPath}", "-C", layout.WorktreeRoot, "push", "--porcelain", "--no-verify", "origin", $"{expectedRef}:{expectedRef}"]), token);
                return push.ExitCode == 0 ? Result.Success() : Result.Failure(new DomainError("publication.push_failed", "The controlled Git push failed."));
            }, cancellationToken);
        }
        catch (TimeoutException) { return Result.Failure(new DomainError("publication.push_timeout", "The controlled Git push timed out.")); }
    }

    private static bool SameRemote(Uri actual, Uri expected) => Uri.Compare(actual, expected, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;

    private Result Validate(Session session, GitPublicationTemplate template)
    {
        if (session.Status != SessionStatus.Active) return Result.Failure(new DomainError("publication.invalid_state", "Publication requires an active session."));
        var layout = layouts.Resolve(session.Id);
        if (!Directory.Exists(layout.WorktreeRoot) || !SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.WorktreeRoot, true)) return Result.Failure(new DomainError("session.worktree.unsafe_path", "The controlled session worktree is unavailable or unsafe."));
        return Result.Success();
    }
}