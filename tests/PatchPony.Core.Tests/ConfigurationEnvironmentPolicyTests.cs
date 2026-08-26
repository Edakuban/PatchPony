using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ConfigurationEnvironmentPolicyTests
{
    [Theory]
    [InlineData(ConfigurationEnvironment.Development, false, false)]
    [InlineData(ConfigurationEnvironment.Staging, true, false)]
    [InlineData(ConfigurationEnvironment.Production, true, true)]
    public void For_EnforcesTheEnvironmentMatrix(ConfigurationEnvironment environment, bool reviewerRequired, bool humanApprovalRequired)
    {
        var policy = ConfigurationEnvironmentPolicy.For(environment);

        Assert.True(policy.MergeRequestAllowedAfterValidation);
        Assert.Equal(reviewerRequired, policy.ReviewerRequired);
        Assert.Equal(humanApprovalRequired, policy.ExplicitHumanApprovalRequiredBeforePublication);
    }

    [Fact]
    public void Create_OnlyAssociatesConfigurationRequestsWithEnvironments()
    {
        var config = Request(RequestedChangeKind.Configuration);
        var source = Request(RequestedChangeKind.SourceCode);

        var production = EnvironmentConfigurationChangeRequest.Create(config, ConfigurationEnvironment.Production);
        var rejected = EnvironmentConfigurationChangeRequest.Create(source, ConfigurationEnvironment.Development);

        Assert.True(production.IsSuccess);
        Assert.True(production.Value!.Policy.ExplicitHumanApprovalRequiredBeforePublication);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("config.environment_request.invalid", rejected.Error.Code);
    }

    private static TicketChangeRequest Request(RequestedChangeKind kind)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var target = RequestedChangeTarget.Create("config/settings.json", kind == RequestedChangeKind.Secret ? null : "value").Value!;
        return TicketChangeRequest.Create(ticket, kind, [target]).Value!;
    }
}