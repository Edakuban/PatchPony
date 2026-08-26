namespace PatchPony.Core.Sessions;

/// <summary>Hard, server-side bounds for temporary session lifetimes.</summary>
public static class SessionLimits
{
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(8);

    public static bool HasAllowedDuration(DateTimeOffset createdAt, DateTimeOffset expiresAt) =>
        expiresAt > createdAt && expiresAt - createdAt <= MaximumDuration;
}