using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ConfigurationKeyPolicyServiceTests
{
    [Fact]
    public void Validate_ReadsOnlyServerConfiguredPolicies()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PatchPony:ConfigurationKeyPolicies:Entries:0:ProjectManifestId"] = "patchpony",
            ["PatchPony:ConfigurationKeyPolicies:Entries:0:Environment"] = "Development",
            ["PatchPony:ConfigurationKeyPolicies:Entries:0:Key"] = "config/enabled",
            ["PatchPony:ConfigurationKeyPolicies:Entries:0:ValueType"] = "Boolean"
        }).Build();
        var request = Request("config/enabled", "true");

        var result = new ConfigurationKeyPolicyService(configuration).Validate(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_FailsClosedForInvalidServerConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PatchPony:ConfigurationKeyPolicies:Entries:0:Environment"] = "Unknown" }).Build();

        var result = new ConfigurationKeyPolicyService(configuration).Validate(Request("config/enabled", "true"));

        Assert.Equal("config.key_policy.unconfigured", result.Error.Code);
    }

    private static EnvironmentConfigurationChangeRequest Request(string key, string value)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [RequestedChangeTarget.Create(key, value).Value!]).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, ConfigurationEnvironment.Development).Value!;
    }
}