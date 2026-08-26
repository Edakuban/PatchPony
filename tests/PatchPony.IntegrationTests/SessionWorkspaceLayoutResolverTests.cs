using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class SessionWorkspaceLayoutResolverTests
{
    [Fact]
    public void Resolve_UsesOnlyServerGeneratedPathsBelowTheConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "patchpony-session-layout", Guid.NewGuid().ToString("N"));
        var sessionId = SessionId.New();
        var layout = new SessionWorkspaceLayoutResolver(root).Resolve(sessionId);

        var expectedSessionRoot = Path.Combine(Path.GetFullPath(root), "sessions", sessionId.Value.ToString("N"));
        Assert.Equal(expectedSessionRoot, layout.SessionRoot);
        Assert.Equal(Path.Combine(expectedSessionRoot, "worktree"), layout.WorktreeRoot);
        Assert.Equal(Path.Combine(expectedSessionRoot, "session.json"), layout.MetadataPath);
        Assert.Equal(Path.Combine(expectedSessionRoot, ".lock"), layout.LockPath);
        Assert.Equal(Path.Combine(expectedSessionRoot, ".disabled-hooks"), layout.DisabledHooksPath);
        Assert.StartsWith(Path.GetFullPath(root), layout.SessionRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolver_RejectsRelativeRootsAndEmptySessionIds()
    {
        Assert.Throws<ArgumentException>(() => new SessionWorkspaceLayoutResolver("relative/sessions"));
        var resolver = new SessionWorkspaceLayoutResolver(Path.GetTempPath());

        Assert.Throws<ArgumentException>(() => resolver.Resolve(new SessionId(Guid.Empty)));
    }
}