using System.Text.Json;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Sandbox;

namespace PatchPony.Gateway;

public sealed class TestSessionOptions
{
    public const string SectionName = "PatchPony:TestSessions";
    public TestSessionEntryOptions[] Sessions { get; init; } = [];
    public void Validate()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Sessions)
        {
            if (!PilotSourceProjectOptions.IdPattern.IsMatch(entry.ProjectId) || !Guid.TryParseExact(entry.SessionId, "N", out _) ||
                !Path.IsPathFullyQualified(entry.WorktreeRoot) || entry.Commands.Length == 0 || !keys.Add($"{entry.ProjectId}:{entry.SessionId}") ||
                entry.Commands.Any(command => !CommandPattern.IsMatch(command)))
            {
                throw new InvalidOperationException("Test session configuration is invalid.");
            }
        }
    }
    internal static readonly Regex CommandPattern = new("^[a-z][a-z0-9._-]{0,127}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}

public sealed class TestSessionEntryOptions
{
    public string ProjectId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string WorktreeRoot { get; init; } = string.Empty;
    public string[] Commands { get; init; } = [];
}

public sealed record TestCatalogResponse(IReadOnlyList<string> Commands);
public sealed record TestRunCommand(string CommandId);
public sealed record TestRunRequest(Guid ExecutionId, string SessionId, string CommandId, DateTimeOffset RequestedAt);

/// <summary>Gateway-only session catalog. It never starts Docker; requests are handed to the private worker channel.</summary>
public sealed class TestSessionCatalog(TestSessionOptions options)
{
    private readonly IReadOnlyDictionary<(string ProjectId, string SessionId), TestSession> sessions = Build(options);

    public Result<TestCatalogResponse> List(string projectId, string sessionId) =>
        sessions.TryGetValue((projectId, sessionId), out var session)
            ? Result<TestCatalogResponse>.Success(new TestCatalogResponse(session.Commands.OrderBy(command => command, StringComparer.Ordinal).ToArray()))
            : Result<TestCatalogResponse>.Failure(new DomainError("session.not_found", "The requested test session is not available."));

    public Result<TestRunRequest> Run(string projectId, string sessionId, TestRunCommand command, CancellationToken cancellationToken = default)
    {
        if (!sessions.TryGetValue((projectId, sessionId), out var session)) return Result<TestRunRequest>.Failure(new DomainError("session.not_found", "The requested test session is not available."));
        if (!TestSessionOptions.CommandPattern.IsMatch(command.CommandId ?? string.Empty) || !session.Commands.Contains(command.CommandId, StringComparer.Ordinal))
            return Result<TestRunRequest>.Failure(new DomainError("sandbox.command.unsupported", "The requested test command is not registered for this session."));

        try
        {
            var request = new TestRunRequest(Guid.NewGuid(), sessionId, command.CommandId!, DateTimeOffset.UtcNow);
            var handoff = Path.Combine(session.SessionRoot, "sandbox-requests");
            Directory.CreateDirectory(handoff);
            var target = Path.Combine(handoff, $"{request.ExecutionId:N}.json");
            File.WriteAllText(target, JsonSerializer.Serialize(request));
            return Result<TestRunRequest>.Success(request);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<TestRunRequest>.Failure(new DomainError("sandbox.request_unavailable", "The test request could not be handed to the worker."));
        }
    }

    public Result<SandboxExecutionResult> Result(string projectId, string sessionId, string executionId)
    {
        if (!sessions.TryGetValue((projectId, sessionId), out var session)) return Result<SandboxExecutionResult>.Failure(new DomainError("session.not_found", "The requested test session is not available."));
        if (!Guid.TryParseExact(executionId, "N", out var id)) return Result<SandboxExecutionResult>.Failure(DomainError.Validation("A valid execution identifier is required."));
        try
        {
            var path = Path.Combine(session.SessionRoot, "sandbox-results", $"{id:N}.json");
            if (!File.Exists(path)) return Result<SandboxExecutionResult>.Failure(new DomainError("sandbox.result.not_found", "The requested test result is not available."));
            var result = JsonSerializer.Deserialize<SandboxExecutionResult>(File.ReadAllText(path));
            return result is null || result.ExecutionId != id ? Result<SandboxExecutionResult>.Failure(new DomainError("sandbox.result.invalid", "The stored test result is invalid.")) : Result<SandboxExecutionResult>.Success(result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Result<SandboxExecutionResult>.Failure(new DomainError("sandbox.result_unavailable", "The stored test result could not be read."));
        }
    }

    private static IReadOnlyDictionary<(string, string), TestSession> Build(TestSessionOptions options)
    {
        var mapped = new Dictionary<(string, string), TestSession>();
        foreach (var entry in options.Sessions)
        {
            if (!Directory.Exists(entry.WorktreeRoot)) throw new InvalidOperationException("A configured test session is unavailable.");
            var sessionRoot = Directory.GetParent(Path.GetFullPath(entry.WorktreeRoot))?.FullName ?? throw new InvalidOperationException("A configured test session is invalid.");
            mapped.Add((entry.ProjectId, entry.SessionId), new TestSession(sessionRoot, entry.Commands.ToHashSet(StringComparer.Ordinal)));
        }
        return mapped;
    }

    private sealed record TestSession(string SessionRoot, IReadOnlySet<string> Commands);
}