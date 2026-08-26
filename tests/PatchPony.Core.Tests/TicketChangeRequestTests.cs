using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketChangeRequestTests
{
    [Fact]
    public void Create_CapturesTypedConfigChangeForChangeRequest()
    {
        var ticket = Ticket(TicketChannel.ChangeRequest);
        var target = RequestedChangeTarget.Create("config/features.json", "enabled=true").Value!;

        var result = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, [target]);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestedChangeKind.Configuration, result.Value!.Kind);
        Assert.Equal("config/features.json", result.Value.Targets[0].Reference);
        Assert.Equal("enabled=true", result.Value.Targets[0].RequestedValue);
    }

    [Fact]
    public void Create_RejectsNonChangeChannelsUnsafeTargetsAndSecretValues()
    {
        var target = RequestedChangeTarget.Create("config/features.json", "enabled=true").Value!;

        Assert.False(TicketChangeRequest.Create(Ticket(TicketChannel.Bug), RequestedChangeKind.Configuration, [target]).IsSuccess);
        Assert.False(RequestedChangeTarget.Create("../.env", "x").IsSuccess);
        Assert.False(TicketChangeRequest.Create(Ticket(TicketChannel.ChangeRequest), RequestedChangeKind.Secret, [target]).IsSuccess);
    }

    private static ProjectMappedTicket Ticket(TicketChannel channel) => ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: channel).Value!, "patchpony").Value!;
}