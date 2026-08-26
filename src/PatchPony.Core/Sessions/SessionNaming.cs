namespace PatchPony.Core.Sessions;

public sealed record ServerSessionNames(string DirectoryName, string BranchName);

public static class SessionNaming
{
    private const string BranchPrefix = "patchpony/session/";

    public static ServerSessionNames For(SessionId sessionId)
    {
        if (sessionId.Value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty session identifier is required.", nameof(sessionId));
        }

        var identifier = sessionId.Value.ToString("N");
        return new ServerSessionNames(identifier, $"{BranchPrefix}{identifier}");
    }
}