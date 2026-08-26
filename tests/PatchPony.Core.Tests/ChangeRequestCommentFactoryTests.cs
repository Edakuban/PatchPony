using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ChangeRequestCommentFactoryTests
{
    [Fact]
    public void Create_RendersOnlyTheEnvironmentDependentReviewStatus()
    {
        var request = Request(ConfigurationEnvironment.Production);
        var review = new ConfigurationReviewRequirement(["release-owner"], true);

        var result = ChangeRequestCommentFactory.Create(request, review);

        Assert.True(result.IsSuccess);
        Assert.Contains("explizite menschliche Freigabe", result.Value!.Outcome, StringComparison.Ordinal);
        Assert.Empty(result.Value.Links);
    }

    [Fact]
    public void Create_RejectsMismatchedReviewRequirements()
    {
        var result = ChangeRequestCommentFactory.Create(Request(ConfigurationEnvironment.Staging), new ConfigurationReviewRequirement([], false));

        Assert.Equal("change_request.comment.invalid", result.Error.Code);
    }

    private static EnvironmentConfigurationChangeRequest Request(ConfigurationEnvironment environment)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [RequestedChangeTarget.Create("config/enabled", "true").Value!]).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, environment).Value!;
    }
}