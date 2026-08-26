using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Knowledge;

public sealed record KnowledgeAreaOwnershipRule(string ProjectId, string PathPattern, IReadOnlyList<string> Owners, IReadOnlyList<string> Reviewers);
public sealed record KnowledgeAreaReviewRequirement(IReadOnlyList<string> Owners, IReadOnlyList<string> Reviewers, string MatchedPathPattern);

/// <summary>Server-owned default-deny mapping from one project vault area to accountable owners and independent reviewers.</summary>
public sealed class KnowledgeAreaOwnershipPolicy
{
    private static readonly Regex Project = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex Person = new("^[A-Za-z0-9-]{1,39}$", RegexOptions.CultureInvariant);
    private readonly IReadOnlyList<KnowledgeAreaOwnershipRule> rules;

    public KnowledgeAreaOwnershipPolicy(IEnumerable<KnowledgeAreaOwnershipRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var validated = new List<KnowledgeAreaOwnershipRule>();
        foreach (var rule in rules)
        {
            if (rule is null || !Project.IsMatch(rule.ProjectId) || !IsSafePattern(rule.PathPattern)) throw new ArgumentException("Knowledge ownership rules require safe project and path identifiers.", nameof(rules));
            var owners = NormalizePeople(rule.Owners);
            var reviewers = NormalizePeople(rule.Reviewers);
            if (owners.Length == 0 || reviewers.Length == 0 || owners.Intersect(reviewers, StringComparer.OrdinalIgnoreCase).Any()) throw new ArgumentException("Knowledge ownership rules require independent owners and reviewers.", nameof(rules));
            if (validated.Any(existing => existing.ProjectId == rule.ProjectId && existing.PathPattern == rule.PathPattern)) throw new ArgumentException("Knowledge ownership rules must be unique per project path pattern.", nameof(rules));
            validated.Add(new KnowledgeAreaOwnershipRule(rule.ProjectId, rule.PathPattern, owners, reviewers));
        }
        this.rules = validated;
    }

    public Result<KnowledgeAreaReviewRequirement> Resolve(string projectId, string path)
    {
        if (!Project.IsMatch(projectId) || !IsSafePath(path)) return Result<KnowledgeAreaReviewRequirement>.Failure(new DomainError("knowledge.ownership.invalid_path", "A safe project and knowledge path are required."));
        var matches = rules.Where(rule => rule.ProjectId == projectId && GlobMatches(rule.PathPattern, path)).OrderByDescending(rule => Specificity(rule.PathPattern)).ToArray();
        if (matches.Length == 0) return Result<KnowledgeAreaReviewRequirement>.Failure(new DomainError("knowledge.ownership.missing", "No knowledge owner and reviewer rule is configured for this path."));
        if (matches.Length > 1 && Specificity(matches[0].PathPattern) == Specificity(matches[1].PathPattern)) return Result<KnowledgeAreaReviewRequirement>.Failure(new DomainError("knowledge.ownership.ambiguous", "The knowledge owner and reviewer rule is ambiguous."));
        var selected = matches[0];
        return Result<KnowledgeAreaReviewRequirement>.Success(new KnowledgeAreaReviewRequirement(selected.Owners, selected.Reviewers, selected.PathPattern));
    }

    private static string[] NormalizePeople(IReadOnlyList<string>? people) => people is null || people.Count is 0 or > 16 || people.Any(person => string.IsNullOrWhiteSpace(person) || !Person.IsMatch(person)) ? [] : people.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(person => person, StringComparer.OrdinalIgnoreCase).ToArray();
    private static bool IsSafePattern(string pattern) => !string.IsNullOrWhiteSpace(pattern) && pattern.Length <= 512 && !pattern.StartsWith("/", StringComparison.Ordinal) && !pattern.Contains('\\') && !pattern.Split('/').Any(segment => segment == "..");
    private static bool IsSafePath(string path) => !string.IsNullOrWhiteSpace(path) && path.Length <= 512 && !path.StartsWith("/", StringComparison.Ordinal) && !path.Contains('\\') && !path.Split('/').Any(segment => segment == "..");
    private static int Specificity(string pattern) => pattern.Count(character => character is not '*' and not '?');
    private static bool GlobMatches(string pattern, string path)
    {
        var memo = new Dictionary<(int Pattern, int Path), bool>();
        return Matches(0, 0);
        bool Matches(int patternIndex, int pathIndex)
        {
            if (memo.TryGetValue((patternIndex, pathIndex), out var cached)) return cached;
            var result = patternIndex == pattern.Length ? pathIndex == path.Length : pattern[patternIndex] == '*'
                ? MatchStar(patternIndex, pathIndex)
                : pattern[patternIndex] == '?' ? pathIndex < path.Length && path[pathIndex] != '/' && Matches(patternIndex + 1, pathIndex + 1)
                : pathIndex < path.Length && pattern[patternIndex] == path[pathIndex] && Matches(patternIndex + 1, pathIndex + 1);
            memo[(patternIndex, pathIndex)] = result;
            return result;
        }
        bool MatchStar(int patternIndex, int pathIndex)
        {
            var doubleStar = patternIndex + 1 < pattern.Length && pattern[patternIndex + 1] == '*';
            var next = patternIndex + (doubleStar ? 2 : 1);
            return (doubleStar && next < pattern.Length && pattern[next] == '/' && Matches(next + 1, pathIndex)) || Matches(next, pathIndex) || (pathIndex < path.Length && (doubleStar || path[pathIndex] != '/') && Matches(patternIndex, pathIndex + 1));
        }
    }
}