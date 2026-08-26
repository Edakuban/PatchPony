using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum ConfigurationEnvironment { Development, Staging, Production }

/// <summary>Non-bypassable baseline requirements for a configuration change in one environment.</summary>
public sealed record ConfigurationEnvironmentPolicy(
    ConfigurationEnvironment Environment,
    bool MergeRequestAllowedAfterValidation,
    bool ReviewerRequired,
    bool ExplicitHumanApprovalRequiredBeforePublication)
{
    public static ConfigurationEnvironmentPolicy For(ConfigurationEnvironment environment) => environment switch
    {
        ConfigurationEnvironment.Development => new(environment, true, false, false),
        ConfigurationEnvironment.Staging => new(environment, true, true, false),
        ConfigurationEnvironment.Production => new(environment, true, true, true),
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };
}

/// <summary>Associates a typed configuration request with a declared target environment and its hard policy.</summary>
public sealed record EnvironmentConfigurationChangeRequest(
    TicketChangeRequest ChangeRequest,
    ConfigurationEnvironment Environment,
    ConfigurationEnvironmentPolicy Policy)
{
    public static Result<EnvironmentConfigurationChangeRequest> Create(TicketChangeRequest changeRequest, ConfigurationEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(changeRequest);
        if (changeRequest.Kind != RequestedChangeKind.Configuration)
            return Result<EnvironmentConfigurationChangeRequest>.Failure(new DomainError("config.environment_request.invalid", "Only configuration change requests can target an environment."));
        return Result<EnvironmentConfigurationChangeRequest>.Success(new EnvironmentConfigurationChangeRequest(changeRequest, environment, ConfigurationEnvironmentPolicy.For(environment)));
    }
}