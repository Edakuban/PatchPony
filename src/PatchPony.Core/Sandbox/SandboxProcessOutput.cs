using PatchPony.Core.Sessions;

namespace PatchPony.Core.Sandbox;

/// <summary>Bounded console output captured from one sandbox process.</summary>
public sealed record SandboxProcessOutput(string StandardOutput, string StandardError, bool StandardOutputTruncated, bool StandardErrorTruncated, bool TimedOut, int? ExitCode);

/// <summary>Completion is intentionally asynchronous because a sandbox job is accepted before its container exits.</summary>
public sealed record SandboxProcessOutputCapture(Task<SandboxProcessOutput> Completion);
public enum SandboxTestOutcome
{
    Passed,
    Failed,
    TimedOut
}

public sealed record SandboxArtifactMetadata(string RelativePath, long LengthBytes, string Sha256);

public sealed record SandboxExecutionResult(
    Guid ExecutionId,
    SessionId SessionId,
    string CommandId,
    DateTimeOffset AcceptedAt,
    DateTimeOffset CompletedAt,
    SandboxTestOutcome Outcome,
    SandboxProcessOutput Output,
    IReadOnlyList<SandboxArtifactMetadata> Artifacts);