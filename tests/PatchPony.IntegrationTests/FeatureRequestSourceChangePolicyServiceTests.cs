using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class FeatureRequestSourceChangePolicyServiceTests
{
    [Fact]
    public void Decide_ExposesOnlyPlanOnlyOutcome()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Updated, "1362699000036844130", "18", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.FeatureRequest).Value!, "patchpony").Value!;
        var request = TicketChangeRequest.Create(ticket, RequestedChangeKind.SourceCode, [RequestedChangeTarget.Create("src/PatchPony.Core/Feature.cs", null).Value!]).Value!;

        var result = new FeatureRequestSourceChangePolicyService().Decide(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketWorkflowOutcome.PlanOnly, result.Value!.Outcome);
    }
}