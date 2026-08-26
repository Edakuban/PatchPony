using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Sandbox;

/// <summary>An immutable, digest-pinned runner image selected only by a server-side identifier.</summary>
public sealed record SandboxRunnerImage(string Id, string ImageReference, bool RootlessCompatible);

public interface ISandboxRunnerCatalog
{
    Result<SandboxRunnerImage> Resolve(string runnerId);
}

/// <summary>Closed server-composed catalog. Image names and tags never originate from sandbox requests.</summary>
public sealed class SandboxRunnerCatalog : ISandboxRunnerCatalog
{
    private static readonly Regex IdPattern = new("^[a-z][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex DigestReferencePattern = new("^[a-z0-9][a-z0-9./_-]{0,255}@sha256:[a-f0-9]{64}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private readonly IReadOnlyDictionary<string, SandboxRunnerImage> runners;

    public SandboxRunnerCatalog(IEnumerable<SandboxRunnerImage> runners)
    {
        var mapped = new Dictionary<string, SandboxRunnerImage>(StringComparer.Ordinal);
        foreach (var runner in runners)
        {
            if (!IdPattern.IsMatch(runner.Id) || !DigestReferencePattern.IsMatch(runner.ImageReference) || !runner.RootlessCompatible || !mapped.TryAdd(runner.Id, runner))
            {
                throw new ArgumentException("Runner images must use unique server-side IDs, lowercase sha256-pinned references and explicit rootless compatibility.", nameof(runners));
            }
        }

        this.runners = mapped;
    }

    public Result<SandboxRunnerImage> Resolve(string runnerId) =>
        runners.TryGetValue(runnerId, out var runner)
            ? Result<SandboxRunnerImage>.Success(runner)
            : Result<SandboxRunnerImage>.Failure(new DomainError("sandbox.runner.unsupported", "The requested sandbox runner is not registered."));
}