using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

/// <summary>Gateway facade that applies the fixed configuration environment matrix.</summary>
public sealed class ConfigurationEnvironmentPolicyService
{
    public Result<EnvironmentConfigurationChangeRequest> Apply(TicketChangeRequest changeRequest, ConfigurationEnvironment environment) => EnvironmentConfigurationChangeRequest.Create(changeRequest, environment);
}