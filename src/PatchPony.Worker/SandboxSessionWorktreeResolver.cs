using PatchPony.Core.Common;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.Worker;

public sealed record SandboxSessionWorktree(string HostPath)
{
    public const string ContainerPath = "/workspace";
}

public interface ISandboxSessionWorktreeResolver
{
    Result<SandboxSessionWorktree> Resolve(SessionId sessionId);
}

/// <summary>Resolves the only writable host path from the server-derived session layout.</summary>
public sealed class SandboxSessionWorktreeResolver(SessionWorkspaceLayoutResolver layouts) : ISandboxSessionWorktreeResolver
{
    public Result<SandboxSessionWorktree> Resolve(SessionId sessionId)
    {
        try
        {
            var layout = layouts.Resolve(sessionId);
            if (!SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.WorktreeRoot, requireDirectory: true))
            {
                return Result<SandboxSessionWorktree>.Failure(new DomainError("sandbox.workspace.unavailable", "The session worktree is unavailable or unsafe to mount."));
            }

            return Result<SandboxSessionWorktree>.Success(new SandboxSessionWorktree(layout.WorktreeRoot));
        }
        catch (ArgumentException)
        {
            return Result<SandboxSessionWorktree>.Failure(new DomainError("sandbox.workspace.unavailable", "The session worktree is unavailable or unsafe to mount."));
        }
        catch (InvalidOperationException)
        {
            return Result<SandboxSessionWorktree>.Failure(new DomainError("sandbox.workspace.unavailable", "The session worktree is unavailable or unsafe to mount."));
        }
    }
}

public sealed class SandboxWorkspaceOptions
{
    public const string SectionName = "PatchPony:Worker:SandboxWorkspace";
    public string StorageRoot { get; init; } = string.Empty;

    public SessionWorkspaceLayoutResolver CreateLayoutResolver()
    {
        if (string.IsNullOrWhiteSpace(StorageRoot) || !Path.IsPathFullyQualified(StorageRoot))
        {
            throw new InvalidOperationException("Sandbox workspace storage root must be an absolute server-side path.");
        }

        return new SessionWorkspaceLayoutResolver(StorageRoot);
    }
}