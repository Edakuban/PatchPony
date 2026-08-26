using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ConfigurationReviewPolicyOptions
{
    public const string SectionName = "PatchPony:ConfigurationReviewPolicies";
    public ConfigurationReviewPolicyEntryOptions[] Entries { get; init; } = [];
}

public sealed class ConfigurationReviewPolicyEntryOptions
{
    public string ProjectManifestId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string[] Reviewers { get; init; } = [];
}

public sealed class ConfigurationReviewPolicyService(IConfiguration configuration)
{
    public Result<ConfigurationReviewRequirement> Resolve(EnvironmentConfigurationChangeRequest request)
    {
        var configured = configuration.GetSection(ConfigurationReviewPolicyOptions.SectionName).Get<ConfigurationReviewPolicyOptions>() ?? new ConfigurationReviewPolicyOptions();
        var policies = new List<ConfigurationEnvironmentReviewPolicy>();
        foreach (var entry in configured.Entries)
        {
            if (!Enum.TryParse<ConfigurationEnvironment>(entry.Environment, true, out var environment)) return Result<ConfigurationReviewRequirement>.Failure(new DomainError("config.review_policy.invalid", "The configuration review policy is invalid."));
            var policy = ConfigurationEnvironmentReviewPolicy.Create(entry.ProjectManifestId, environment, entry.Reviewers);
            if (!policy.IsSuccess) return Result<ConfigurationReviewRequirement>.Failure(policy.Error);
            policies.Add(policy.Value!);
        }
        return ConfigurationReviewPolicyResolver.Resolve(request, policies);
    }
}