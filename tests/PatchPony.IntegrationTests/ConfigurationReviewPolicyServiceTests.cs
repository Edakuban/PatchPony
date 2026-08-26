using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ConfigurationReviewPolicyServiceTests
{
    [Fact]
    public void Resolve_ReadsServerConfiguredReviewers()
    {
        var values = new Dictionary<string, string?>
        {
            ["PatchPony:ConfigurationReviewPolicies:Entries:0:ProjectManifestId"] = "patchpony",
            ["PatchPony:ConfigurationReviewPolicies:Entries:0:Environment"] = "Staging",
            ["PatchPony:ConfigurationReviewPolicies:Entries:0:Reviewers:0"] = "alice",
            ["PatchPony:ConfigurationReviewPolicies:Entries:0:Reviewers:1"] = "bob"
        };

        var result = new ConfigurationReviewPolicyService(new ConfigurationBuilder().AddInMemoryCollection(values).Build()).Resolve(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(["alice", "bob"], result.Value!.Reviewers);
    }

    private static EnvironmentConfigurationChangeRequest Request()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [RequestedChangeTarget.Create("config/enabled", "true").Value!]).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, ConfigurationEnvironment.Staging).Value!;
    }
}