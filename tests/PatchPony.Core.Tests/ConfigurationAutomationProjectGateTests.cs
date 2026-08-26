using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ConfigurationAutomationProjectGateTests
{
    [Fact]
    public void EnsureAllowed_RequiresOneExplicitEnabledProjectPolicy()
    {
        var request = Request("patchpony");
        var enabled = ConfigurationAutomationProjectPolicy.Create("patchpony", true).Value!;
        var disabled = ConfigurationAutomationProjectPolicy.Create("patchpony", false).Value!;

        Assert.True(ConfigurationAutomationProjectGate.EnsureAllowed(request, [enabled]).IsSuccess);
        Assert.Equal("config.automation.not_allowed", ConfigurationAutomationProjectGate.EnsureAllowed(request, [disabled]).Error.Code);
        Assert.Equal("config.automation_policy.unconfigured", ConfigurationAutomationProjectGate.EnsureAllowed(request, []).Error.Code);
    }

    [Fact]
    public void EnsureAllowed_FailsClosedForAmbiguousPolicies()
    {
        var request = Request("patchpony");
        var policy = ConfigurationAutomationProjectPolicy.Create("patchpony", true).Value!;

        var result = ConfigurationAutomationProjectGate.EnsureAllowed(request, [policy, policy]);

        Assert.Equal("config.automation_policy.unconfigured", result.Error.Code);
    }

    private static EnvironmentConfigurationChangeRequest Request(string project) 
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, project).Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [RequestedChangeTarget.Create("config/enabled", "true").Value!]).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, ConfigurationEnvironment.Development).Value!;
    }
}