using PatchPony.Core.Common;

namespace PatchPony.Core.Sessions;

/// <summary>Infrastructure boundary for discarding a server-derived session worktree.</summary>
public interface ISessionWorkspaceDiscarder
{
    Task<Result> DiscardAsync(Session session, string baseCheckoutRoot, CancellationToken cancellationToken = default);
}