using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ConfigurationKeyPolicyOptions
{
    public const string SectionName = "PatchPony:ConfigurationKeyPolicies";
    public ConfigurationKeyPolicyEntryOptions[] Entries { get; init; } = [];
}

public sealed class ConfigurationKeyPolicyEntryOptions
{
    public string ProjectManifestId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string ValueType { get; init; } = string.Empty;
    public long? Minimum { get; init; }
    public long? Maximum { get; init; }
    public int? MaximumLength { get; init; }
    public string[] AllowedValues { get; init; } = [];
}

public sealed class ConfigurationKeyPolicyService(IConfiguration configuration)
{
    public Result<ConfigurationChangeKeyValidation> Validate(EnvironmentConfigurationChangeRequest request)
    {
        var configured = configuration.GetSection(ConfigurationKeyPolicyOptions.SectionName).Get<ConfigurationKeyPolicyOptions>() ?? new ConfigurationKeyPolicyOptions();
        var policies = new List<KnownConfigurationKeyPolicy>();
        foreach (var entry in configured.Entries)
        {
            if (!Enum.TryParse<ConfigurationEnvironment>(entry.Environment, true, out var environment) || !Enum.TryParse<ConfigurationValueType>(entry.ValueType, true, out var valueType))
                return Result<ConfigurationChangeKeyValidation>.Failure(new DomainError("config.key_policy.unconfigured", "No configuration key policy is configured."));
            var policy = KnownConfigurationKeyPolicy.Create(entry.ProjectManifestId, environment, entry.Key, valueType, entry.Minimum, entry.Maximum, entry.MaximumLength, entry.AllowedValues);
            if (!policy.IsSuccess) return Result<ConfigurationChangeKeyValidation>.Failure(new DomainError("config.key_policy.unconfigured", "No configuration key policy is configured."));
            policies.Add(policy.Value!);
        }
        return ConfigurationChangeKeyValidator.Validate(request, policies);
    }
}