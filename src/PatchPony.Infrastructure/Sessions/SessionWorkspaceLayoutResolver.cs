using PatchPony.Core.Sessions;

namespace PatchPony.Infrastructure.Sessions;

public sealed record SessionWorkspaceLayout(
    string StorageRoot,
    string SessionRoot,
    string WorktreeRoot,
    string MetadataPath,
    string LockPath,
    string DisabledHooksPath);

public sealed class SessionWorkspaceLayoutResolver
{
    private const string SessionsDirectoryName = "sessions";
    private readonly string storageRoot;

    public SessionWorkspaceLayoutResolver(string storageRoot)
    {
        if (!Path.IsPathFullyQualified(storageRoot))
        {
            throw new ArgumentException("The session storage root must be an absolute server-side path.", nameof(storageRoot));
        }

        this.storageRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(storageRoot));
    }

    public SessionWorkspaceLayout Resolve(SessionId sessionId)
    {
        if (sessionId.Value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty session identifier is required.", nameof(sessionId));
        }

        var names = SessionNaming.For(sessionId);
        var sessionRoot = Path.Combine(storageRoot, SessionsDirectoryName, names.DirectoryName);
        EnsureInsideStorageRoot(sessionRoot);
        return new SessionWorkspaceLayout(
            storageRoot,
            sessionRoot,
            Path.Combine(sessionRoot, "worktree"),
            Path.Combine(sessionRoot, "session.json"),
            Path.Combine(sessionRoot, ".lock"),
            Path.Combine(sessionRoot, ".disabled-hooks"));
    }

    private void EnsureInsideStorageRoot(string candidate)
    {
        var relative = Path.GetRelativePath(storageRoot, candidate);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The generated session layout is outside the configured storage root.");
        }
    }
}