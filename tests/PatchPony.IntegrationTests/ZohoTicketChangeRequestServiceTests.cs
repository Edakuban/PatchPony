using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ZohoTicketChangeRequestServiceTests
{
    [Fact]
    public void Create_DelegatesOnlyValidatedTypedRequests()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Updated, "1362699000036844130", "18", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.FeatureRequest).Value!, "patchpony").Value!;
        var target = RequestedChangeTarget.Create("src/PatchPony.Core/Feature.cs", null).Value!;

        var result = new ZohoTicketChangeRequestService().Create(ticket, RequestedChangeKind.SourceCode, [target]);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketChannel.FeatureRequest, result.Value!.Channel);
    }
}