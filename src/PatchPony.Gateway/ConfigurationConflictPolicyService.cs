using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ConfigurationConflictPolicyOptions
{
    public const string SectionName = "PatchPony:ConfigurationConflictPolicies";
    public ConfigurationConflictPolicyEntryOptions[] Entries { get; init; } = [];
}

public sealed class ConfigurationConflictPolicyEntryOptions
{
    public string ProjectManifestId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string LeftKey { get; init; } = string.Empty;
    public string LeftValue { get; init; } = string.Empty;
    public string RightKey { get; init; } = string.Empty;
    public string RightValue { get; init; } = string.Empty;
}

public sealed class ConfigurationConflictPolicyService(IConfiguration configuration)
{
    public Result Validate(EnvironmentConfigurationChangeRequest request, ConfigurationChangeKeyValidation keyValidation)
    {
        var configured = configuration.GetSection(ConfigurationConflictPolicyOptions.SectionName).Get<ConfigurationConflictPolicyOptions>() ?? new ConfigurationConflictPolicyOptions();
        var rules = new List<ConfigurationConflictRule>();
        foreach (var entry in configured.Entries)
        {
            if (!Enum.TryParse<ConfigurationEnvironment>(entry.Environment, true, out var environment)) return Result.Failure(new DomainError("config.conflict_policy.invalid", "The configuration conflict policy is invalid."));
            var rule = ConfigurationConflictRule.Create(entry.ProjectManifestId, environment, entry.LeftKey, entry.LeftValue, entry.RightKey, entry.RightValue);
            if (!rule.IsSuccess) return Result.Failure(rule.Error);
            rules.Add(rule.Value!);
        }
        return ConfigurationChangeConflictDetector.Validate(request, keyValidation, rules);
    }
}