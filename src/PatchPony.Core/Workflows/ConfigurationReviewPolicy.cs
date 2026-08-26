using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public sealed record ConfigurationEnvironmentReviewPolicy(string ProjectManifestId, ConfigurationEnvironment Environment, IReadOnlyList<string> Reviewers)
{
    private static readonly Regex ReviewerPattern = new("^[A-Za-z0-9-]{1,39}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static Result<ConfigurationEnvironmentReviewPolicy> Create(string projectManifestId, ConfigurationEnvironment environment, IReadOnlyList<string>? reviewers)
    {
        var safeReviewers = reviewers ?? [];
        if (!IsProjectId(projectManifestId) || environment == ConfigurationEnvironment.Development || safeReviewers.Count is < 1 or > 16 || safeReviewers.Any(reviewer => string.IsNullOrWhiteSpace(reviewer) || !ReviewerPattern.IsMatch(reviewer)) || safeReviewers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != safeReviewers.Count)
            return Result<ConfigurationEnvironmentReviewPolicy>.Failure(new DomainError("config.review_policy.invalid", "The configuration review policy is invalid."));
        return Result<ConfigurationEnvironmentReviewPolicy>.Success(new ConfigurationEnvironmentReviewPolicy(projectManifestId, environment, safeReviewers.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()));
    }

    private static bool IsProjectId(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 63 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}

public sealed record ConfigurationReviewRequirement(IReadOnlyList<string> Reviewers, bool ExplicitHumanApprovalRequiredBeforePublication);

/// <summary>Resolves reviewer and approval requirements from the fixed environment matrix plus server-owned policies.</summary>
public static class ConfigurationReviewPolicyResolver
{
    public static Result<ConfigurationReviewRequirement> Resolve(EnvironmentConfigurationChangeRequest request, IReadOnlyList<ConfigurationEnvironmentReviewPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policies);
        var matches = policies.Where(policy => policy.ProjectManifestId == request.ChangeRequest.ProjectManifestId && policy.Environment == request.Environment).ToArray();
        if (policies.Any(policy => policy is null) || matches.Length > 1) return Result<ConfigurationReviewRequirement>.Failure(new DomainError("config.review_policy.invalid", "The configuration review policy is invalid."));
        if (!request.Policy.ReviewerRequired)
        {
            return matches.Length == 0
                ? Result<ConfigurationReviewRequirement>.Success(new ConfigurationReviewRequirement([], request.Policy.ExplicitHumanApprovalRequiredBeforePublication))
                : Result<ConfigurationReviewRequirement>.Failure(new DomainError("config.review_policy.invalid", "The configuration review policy is invalid."));
        }
        if (matches.Length != 1) return Result<ConfigurationReviewRequirement>.Failure(new DomainError("config.review_policy.missing", "No configuration reviewer policy is configured."));
        return Result<ConfigurationReviewRequirement>.Success(new ConfigurationReviewRequirement(matches[0].Reviewers, request.Policy.ExplicitHumanApprovalRequiredBeforePublication));
    }
}