using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ConfigurationAutomationProjectOptions
{
    public const string SectionName = "PatchPony:ConfigurationAutomationProjects";
    public ConfigurationAutomationProjectEntryOptions[] Entries { get; init; } = [];
}

public sealed class ConfigurationAutomationProjectEntryOptions
{
    public string ProjectManifestId { get; init; } = string.Empty;
    public bool AllowConfigOnlyAutomation { get; init; }
}

public sealed class ConfigurationAutomationProjectGateService(IConfiguration configuration)
{
    public Result EnsureAllowed(EnvironmentConfigurationChangeRequest request)
    {
        var configured = configuration.GetSection(ConfigurationAutomationProjectOptions.SectionName).Get<ConfigurationAutomationProjectOptions>() ?? new ConfigurationAutomationProjectOptions();
        var policies = new List<ConfigurationAutomationProjectPolicy>();
        foreach (var entry in configured.Entries)
        {
            var policy = ConfigurationAutomationProjectPolicy.Create(entry.ProjectManifestId, entry.AllowConfigOnlyAutomation);
            if (!policy.IsSuccess) return Result.Failure(policy.Error);
            policies.Add(policy.Value!);
        }
        return ConfigurationAutomationProjectGate.EnsureAllowed(request, policies);
    }
}