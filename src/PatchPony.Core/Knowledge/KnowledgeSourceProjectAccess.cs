using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Knowledge;

public enum KnowledgePathAccess { Read, Write }

/// <summary>Project-specific read/write boundaries inside a separately registered knowledge vault.</summary>
public sealed record KnowledgeSourceProjectAccess(
    KnowledgeSourceId SourceId,
    ProjectId ProjectId,
    IReadOnlyList<string> ReadablePaths,
    IReadOnlyList<string> WritablePaths)
{
    public static Result<KnowledgeSourceProjectAccess> Create(KnowledgeSourceId sourceId, ProjectId projectId, IReadOnlyList<string>? readablePaths, IReadOnlyList<string>? writablePaths)
    {
        var readable = readablePaths ?? [];
        var writable = writablePaths ?? [];
        if (sourceId.Value == Guid.Empty || projectId.Value == Guid.Empty || readable.Count == 0 || !AreSafeGlobs(readable) || !AreSafeGlobs(writable))
            return Result<KnowledgeSourceProjectAccess>.Failure(new DomainError("knowledge.access_policy.invalid", "The knowledge access policy is invalid."));
        return Result<KnowledgeSourceProjectAccess>.Success(new KnowledgeSourceProjectAccess(sourceId, projectId, readable.Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray(), writable.Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray()));
    }

    public Result Authorize(string relativePath, KnowledgePathAccess access)
    {
        if (!IsSafePath(relativePath)) return Result.Failure(new DomainError("knowledge.path.invalid", "A safe relative knowledge path is required."));
        var patterns = access == KnowledgePathAccess.Read ? ReadablePaths : WritablePaths;
        return patterns.Any(pattern => GlobMatches(pattern, relativePath))
            ? Result.Success()
            : Result.Failure(new DomainError(access == KnowledgePathAccess.Read ? "knowledge.path.not_readable" : "knowledge.path.not_writable", "The knowledge path is not approved for this project."));
    }

    private static bool AreSafeGlobs(IReadOnlyList<string> paths) => paths.All(path => !string.IsNullOrWhiteSpace(path) && path.Length <= 512 && !path.StartsWith("/", StringComparison.Ordinal) && !path.Contains((char)92) && !path.Split('/').Any(segment => segment == ".."));
    private static bool IsSafePath(string path) => !string.IsNullOrWhiteSpace(path) && path.Length <= 4_096 && !path.StartsWith("/", StringComparison.Ordinal) && !path.Contains((char)92) && !path.Split('/').Any(segment => segment == "..");
    private static bool GlobMatches(string pattern, string path)
    {
        var memo = new Dictionary<(int, int), bool>();
        return Matches(0, 0);
        bool Matches(int patternIndex, int pathIndex)
        {
            if (memo.TryGetValue((patternIndex, pathIndex), out var cached)) return cached;
            bool matches;
            if (patternIndex == pattern.Length) matches = pathIndex == path.Length;
            else if (pattern[patternIndex] == '*')
            {
                var doubleStar = patternIndex + 1 < pattern.Length && pattern[patternIndex + 1] == '*';
                var next = patternIndex + (doubleStar ? 2 : 1);
                matches = doubleStar && next < pattern.Length && pattern[next] == '/' && Matches(next + 1, pathIndex);
                matches |= Matches(next, pathIndex);
                matches |= pathIndex < path.Length && (doubleStar || path[pathIndex] != '/') && Matches(patternIndex, pathIndex + 1);
            }
            else if (pattern[patternIndex] == '?') matches = pathIndex < path.Length && path[pathIndex] != '/' && Matches(patternIndex + 1, pathIndex + 1);
            else matches = pathIndex < path.Length && pattern[patternIndex] == path[pathIndex] && Matches(patternIndex + 1, pathIndex + 1);
            memo[(patternIndex, pathIndex)] = matches;
            return matches;
        }
    }
}

/// <summary>Fail-closed catalog that resolves one vault access policy for one project.</summary>
public sealed class KnowledgeSourceAccessCatalog
{
    private readonly IReadOnlyDictionary<(KnowledgeSourceId SourceId, ProjectId ProjectId), KnowledgeSourceProjectAccess> policies;

    public KnowledgeSourceAccessCatalog(IEnumerable<KnowledgeSourceProjectAccess> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        var mapped = new Dictionary<(KnowledgeSourceId, ProjectId), KnowledgeSourceProjectAccess>();
        foreach (var policy in policies)
        {
            if (policy is null || !mapped.TryAdd((policy.SourceId, policy.ProjectId), policy)) throw new ArgumentException("Knowledge access policies must be unique by source and project.", nameof(policies));
        }
        this.policies = mapped;
    }

    public Result Authorize(KnowledgeSourceId sourceId, ProjectId projectId, string relativePath, KnowledgePathAccess access) =>
        policies.TryGetValue((sourceId, projectId), out var policy)
            ? policy.Authorize(relativePath, access)
            : Result.Failure(new DomainError("knowledge.access_policy.missing", "No knowledge access policy is configured for this project."));
}