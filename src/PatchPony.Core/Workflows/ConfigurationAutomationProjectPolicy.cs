using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public sealed record ConfigurationAutomationProjectPolicy(string ProjectManifestId, bool AllowConfigOnlyAutomation)
{
    public static Result<ConfigurationAutomationProjectPolicy> Create(string projectManifestId, bool allowConfigOnlyAutomation)
    {
        if (string.IsNullOrWhiteSpace(projectManifestId) || projectManifestId.Length > 63 || projectManifestId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            return Result<ConfigurationAutomationProjectPolicy>.Failure(new DomainError("config.automation_policy.invalid", "The configuration automation policy is invalid."));
        return Result<ConfigurationAutomationProjectPolicy>.Success(new ConfigurationAutomationProjectPolicy(projectManifestId, allowConfigOnlyAutomation));
    }
}

/// <summary>Explicit project allowlist for otherwise validated configuration-only automation.</summary>
public static class ConfigurationAutomationProjectGate
{
    public static Result EnsureAllowed(EnvironmentConfigurationChangeRequest request, IReadOnlyList<ConfigurationAutomationProjectPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policies);
        var matches = policies.Where(policy => policy.ProjectManifestId == request.ChangeRequest.ProjectManifestId).ToArray();
        if (policies.Any(policy => policy is null) || matches.Length != 1)
            return Result.Failure(new DomainError("config.automation_policy.unconfigured", "No configuration automation policy is configured for this project."));
        return matches[0].AllowConfigOnlyAutomation
            ? Result.Success()
            : Result.Failure(new DomainError("config.automation.not_allowed", "Configuration-only automation is not allowed for this project."));
    }
}