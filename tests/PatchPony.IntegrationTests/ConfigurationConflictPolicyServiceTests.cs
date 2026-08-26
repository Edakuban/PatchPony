using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ConfigurationConflictPolicyServiceTests
{
    [Fact]
    public void Validate_ReadsConfiguredConflictRules()
    {
        var values = new Dictionary<string, string?>
        {
            ["PatchPony:ConfigurationConflictPolicies:Entries:0:ProjectManifestId"] = "patchpony",
            ["PatchPony:ConfigurationConflictPolicies:Entries:0:Environment"] = "Production",
            ["PatchPony:ConfigurationConflictPolicies:Entries:0:LeftKey"] = "config/auth-mode",
            ["PatchPony:ConfigurationConflictPolicies:Entries:0:LeftValue"] = "disabled",
            ["PatchPony:ConfigurationConflictPolicies:Entries:0:RightKey"] = "config/public-access",
            ["PatchPony:ConfigurationConflictPolicies:Entries:0:RightValue"] = "true"
        };
        var request = Request();
        var validation = new ConfigurationChangeKeyValidation(request.ChangeRequest.Targets.Select(target => new ValidatedConfigurationChangeTarget(target.Reference, ConfigurationValueType.Enum)).ToArray());

        var result = new ConfigurationConflictPolicyService(new ConfigurationBuilder().AddInMemoryCollection(values).Build()).Validate(request, validation);

        Assert.Equal("config.values.conflict", result.Error.Code);
    }

    private static EnvironmentConfigurationChangeRequest Request()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var targets = new[] { RequestedChangeTarget.Create("config/auth-mode", "disabled").Value!, RequestedChangeTarget.Create("config/public-access", "true").Value! };
        return EnvironmentConfigurationChangeRequest.Create(TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, targets).Value!, ConfigurationEnvironment.Production).Value!;
    }
}