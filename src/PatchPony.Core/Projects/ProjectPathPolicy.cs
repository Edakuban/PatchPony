using PatchPony.Core.Common;

namespace PatchPony.Core.Projects;

public enum ProjectPathAccess
{
    Read,
    Write
}

public sealed class ProjectPathPolicy
{
    private readonly IReadOnlyList<string> readable;
    private readonly IReadOnlyList<string> writable;
    private readonly IReadOnlyList<string> forbidden;

    private ProjectPathPolicy(ProjectManifestPaths paths)
    {
        readable = paths.Readable;
        writable = paths.Writable;
        forbidden = paths.Forbidden;
    }

    public static Result<ProjectPathPolicy> Create(ProjectManifestPaths paths)
    {
        if (paths is null || !paths.Readable.Any() || !paths.Forbidden.Any() || !AreSafeGlobs(paths.Readable) || !AreSafeGlobs(paths.Writable) || !AreSafeGlobs(paths.Forbidden))
        {
            return Result<ProjectPathPolicy>.Failure(DomainError.Validation("Project path policy contains an invalid glob."));
        }

        return Result<ProjectPathPolicy>.Success(new ProjectPathPolicy(paths));
    }

    public Result Authorize(string relativePath, ProjectPathAccess access)
    {
        if (!IsSafePath(relativePath))
        {
            return Result.Failure(new DomainError("path.invalid", "A safe relative project path is required."));
        }

        if (forbidden.Any(pattern => GlobMatches(pattern, relativePath)))
        {
            return Result.Failure(new DomainError("path.forbidden", "The requested path is forbidden by the project policy."));
        }

        if (access == ProjectPathAccess.Read)
        {
            return readable.Any(pattern => GlobMatches(pattern, relativePath))
                ? Result.Success()
                : Result.Failure(new DomainError("path.not_readable", "The requested path is not readable by the project policy."));
        }

        if (!writable.Any(pattern => GlobMatches(pattern, relativePath)))
        {
            return Result.Failure(new DomainError("path.not_writable", "The requested path is not writable by the project policy."));
        }

        return Result.Failure(new DomainError("path.write_disabled", "Project checkouts are read-only in this iteration."));
    }

    private static bool AreSafeGlobs(IReadOnlyList<string> patterns) => patterns.All(IsSafeGlob);

    private static bool IsSafeGlob(string pattern) =>
        !string.IsNullOrWhiteSpace(pattern) &&
        pattern.Length <= 512 &&
        !pattern.StartsWith("/", StringComparison.Ordinal) &&
        !pattern.Contains('\\') &&
        !pattern.Split('/', StringSplitOptions.None).Any(segment => segment == "..");

    private static bool IsSafePath(string path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.Length <= 4_096 &&
        !path.StartsWith("/", StringComparison.Ordinal) &&
        !path.Contains('\\') &&
        !path.Split('/', StringSplitOptions.None).Any(segment => segment == "..");

    private static bool GlobMatches(string pattern, string path)
    {
        var memo = new Dictionary<(int PatternIndex, int PathIndex), bool>();
        return Matches(0, 0);

        bool Matches(int patternIndex, int pathIndex)
        {
            if (memo.TryGetValue((patternIndex, pathIndex), out var cached))
            {
                return cached;
            }

            bool matches;
            if (patternIndex == pattern.Length)
            {
                matches = pathIndex == path.Length;
            }
            else if (pattern[patternIndex] == '*')
            {
                var isDoubleStar = patternIndex + 1 < pattern.Length && pattern[patternIndex + 1] == '*';
                var nextPatternIndex = patternIndex + (isDoubleStar ? 2 : 1);
                matches = isDoubleStar && nextPatternIndex < pattern.Length && pattern[nextPatternIndex] == '/' && Matches(nextPatternIndex + 1, pathIndex);
                matches |= Matches(nextPatternIndex, pathIndex);
                matches |= pathIndex < path.Length && (isDoubleStar || path[pathIndex] != '/') && Matches(patternIndex, pathIndex + 1);
            }
            else if (pattern[patternIndex] == '?')
            {
                matches = pathIndex < path.Length && path[pathIndex] != '/' && Matches(patternIndex + 1, pathIndex + 1);
            }
            else
            {
                matches = pathIndex < path.Length && pattern[patternIndex] == path[pathIndex] && Matches(patternIndex + 1, pathIndex + 1);
            }

            memo[(patternIndex, pathIndex)] = matches;
            return matches;
        }
    }
}
