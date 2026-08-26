using PatchPony.Core.Common;

namespace PatchPony.Infrastructure.Sessions;

public sealed record WorkspaceSizeMeasurement(long Bytes, bool ExceedsLimit);

/// <summary>Measures a server-derived worktree without following reparse points.</summary>
public sealed class SessionWorkspaceSizeLimiter
{
    public const long DefaultMaximumBytes = 1024L * 1024L * 1024L;
    public long MaximumBytes { get; }

    public SessionWorkspaceSizeLimiter(long maximumBytes = DefaultMaximumBytes)
    {
        if (maximumBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes), "The workspace size limit must be positive.");
        }

        MaximumBytes = maximumBytes;
    }

    public Result<WorkspaceSizeMeasurement> Measure(string storageRoot, string worktreeRoot)
    {
        if (!Path.IsPathFullyQualified(worktreeRoot) || !Directory.Exists(worktreeRoot))
        {
            return Result<WorkspaceSizeMeasurement>.Failure(new DomainError("session.workspace_size_unavailable", "The session worktree is unavailable for size enforcement."));
        }

        if (!SessionWorkspacePathGuard.IsSafePath(storageRoot, worktreeRoot, requireDirectory: true))
        {
            return Result<WorkspaceSizeMeasurement>.Failure(new DomainError("session.worktree.unsafe_path", "The controlled session workspace path is unsafe."));
        }

        try
        {
            long bytes = 0;
            var directories = new Stack<string>();
            directories.Push(Path.GetFullPath(worktreeRoot));
            while (directories.TryPop(out var directory))
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
                {
                    var attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        directories.Push(entry);
                        continue;
                    }

                    var length = new FileInfo(entry).Length;
                    if (length > MaximumBytes - bytes)
                    {
                        return Result<WorkspaceSizeMeasurement>.Success(new WorkspaceSizeMeasurement(MaximumBytes, true));
                    }

                    bytes += length;
                }
            }

            return Result<WorkspaceSizeMeasurement>.Success(new WorkspaceSizeMeasurement(bytes, false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<WorkspaceSizeMeasurement>.Failure(new DomainError("session.workspace_size_unavailable", "The session worktree size could not be measured."));
        }
    }
}