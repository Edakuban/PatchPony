using System.Security.Cryptography;
using System.Text.Json;
using PatchPony.Core.Sandbox;

namespace PatchPony.Worker;

public interface ISandboxExecutionResultRecorder
{
    void Track(SandboxJobAcceptance acceptance);
}

/// <summary>Persists bounded execution results beside the server-derived session worktree.</summary>
public sealed class SandboxExecutionResultRecorder(ISandboxSessionWorktreeResolver worktrees, TimeProvider? clock = null) : ISandboxExecutionResultRecorder
{
    private const string ArtifactDirectoryName = ".patchpony-artifacts";
    private const string ResultDirectoryName = "sandbox-results";
    private const int MaximumArtifactCount = 100;
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    public void Track(SandboxJobAcceptance acceptance)
    {
        if (acceptance.Launch.Output is { } output)
        {
            _ = PersistWhenCompleteAsync(acceptance, output.Completion);
        }
    }

    private async Task PersistWhenCompleteAsync(SandboxJobAcceptance acceptance, Task<SandboxProcessOutput> completion)
    {
        string? temporary = null;
        try
        {
            var output = await completion.ConfigureAwait(false);
            var workspace = worktrees.Resolve(acceptance.SessionId);
            if (!workspace.IsSuccess) return;

            var worktree = workspace.Value!.HostPath;
            var sessionRoot = Directory.GetParent(worktree)?.FullName;
            if (sessionRoot is null) return;

            var result = new SandboxExecutionResult(
                acceptance.ExecutionId,
                acceptance.SessionId,
                acceptance.CommandId,
                acceptance.AcceptedAt,
                clock.GetUtcNow(),
                output.TimedOut ? SandboxTestOutcome.TimedOut : output.ExitCode == 0 ? SandboxTestOutcome.Passed : SandboxTestOutcome.Failed,
                output,
                CollectArtifacts(worktree));

            var resultsDirectory = Path.Combine(sessionRoot, ResultDirectoryName);
            Directory.CreateDirectory(resultsDirectory);
            var destination = Path.Combine(resultsDirectory, $"{acceptance.ExecutionId:N}.json");
            temporary = Path.Combine(resultsDirectory, $".{acceptance.ExecutionId:N}.{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(result)).ConfigureAwait(false);
            File.Move(temporary, destination, overwrite: true);
            temporary = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // Cleanup is attempted below; I9.12 adds runner compatibility verification.
        }
        finally
        {
            if (temporary is not null)
            {
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static IReadOnlyList<SandboxArtifactMetadata> CollectArtifacts(string worktree)
    {
        var root = Path.Combine(worktree, ArtifactDirectoryName);
        if (!Directory.Exists(root) || IsReparsePoint(root)) return [];

        try
        {
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Take(MaximumArtifactCount)
                .Where(path => !IsReparsePoint(path))
                .Select(path => new FileInfo(path))
                .Where(file => file.Length <= 100L * 1024 * 1024)
                .Select(file => new SandboxArtifactMetadata(
                    Path.GetRelativePath(root, file.FullName).Replace(Path.DirectorySeparatorChar, '/'),
                    file.Length,
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file.FullName))).ToLowerInvariant()))
                .ToList();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool IsReparsePoint(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}