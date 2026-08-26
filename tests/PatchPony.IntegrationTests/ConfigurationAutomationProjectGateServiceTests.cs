using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ConfigurationAutomationProjectGateServiceTests
{
    [Fact]
    public void EnsureAllowed_UsesOnlyConfiguredProjectAllowlist()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PatchPony:ConfigurationAutomationProjects:Entries:0:ProjectManifestId"] = "patchpony",
            ["PatchPony:ConfigurationAutomationProjects:Entries:0:AllowConfigOnlyAutomation"] = "true"
        }).Build();

        var result = new ConfigurationAutomationProjectGateService(configuration).EnsureAllowed(Request());

        Assert.True(result.IsSuccess);
    }

    private static EnvironmentConfigurationChangeRequest Request()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [RequestedChangeTarget.Create("config/enabled", "true").Value!]).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, ConfigurationEnvironment.Development).Value!;
    }
}