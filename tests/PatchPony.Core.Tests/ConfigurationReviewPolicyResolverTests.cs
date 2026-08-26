using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ConfigurationReviewPolicyResolverTests
{
    [Fact]
    public void Resolve_UsesNoReviewersForDevelopmentAndRequiresConfiguredReviewersForStaging()
    {
        var development = Request(ConfigurationEnvironment.Development);
        var staging = Request(ConfigurationEnvironment.Staging);
        var policy = ConfigurationEnvironmentReviewPolicy.Create("patchpony", ConfigurationEnvironment.Staging, ["alice", "bob"]).Value!;

        var devResult = ConfigurationReviewPolicyResolver.Resolve(development, []);
        var stagingResult = ConfigurationReviewPolicyResolver.Resolve(staging, [policy]);

        Assert.True(devResult.IsSuccess);
        Assert.Empty(devResult.Value!.Reviewers);
        Assert.True(stagingResult.IsSuccess);
        Assert.Equal(["alice", "bob"], stagingResult.Value!.Reviewers);
        Assert.False(stagingResult.Value.ExplicitHumanApprovalRequiredBeforePublication);
    }

    [Fact]
    public void Resolve_FailsClosedForMissingOrUnexpectedReviewerPolicies()
    {
        var staging = Request(ConfigurationEnvironment.Staging);
        var development = Request(ConfigurationEnvironment.Development);
        var policy = ConfigurationEnvironmentReviewPolicy.Create("patchpony", ConfigurationEnvironment.Staging, ["alice"]).Value!;

        Assert.Equal("config.review_policy.missing", ConfigurationReviewPolicyResolver.Resolve(staging, []).Error.Code);
        Assert.Equal("config.review_policy.invalid", ConfigurationReviewPolicyResolver.Resolve(staging, [policy, policy]).Error.Code);
    }

    [Fact]
    public void Resolve_RequiresReviewersAndExplicitApprovalForProduction()
    {
        var production = Request(ConfigurationEnvironment.Production);
        var policy = ConfigurationEnvironmentReviewPolicy.Create("patchpony", ConfigurationEnvironment.Production, ["release-owner"]).Value!;

        var result = ConfigurationReviewPolicyResolver.Resolve(production, [policy]);

        Assert.True(result.IsSuccess);
        Assert.Equal(["release-owner"], result.Value!.Reviewers);
        Assert.True(result.Value.ExplicitHumanApprovalRequiredBeforePublication);
    }

    private static EnvironmentConfigurationChangeRequest Request(ConfigurationEnvironment environment)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [RequestedChangeTarget.Create("config/enabled", "true").Value!]).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, environment).Value!;
    }
}