using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ZohoTicketNormalizerTests
{
    [Fact]
    public void Normalize_MapsClosedZohoTaskFieldsToTheNeutralContract()
    {
        var now = DateTimeOffset.UtcNow;
        var result = new ZohoTicketNormalizer().Normalize(new ZohoTaskWebhook("task.updated", "1362699000036844130", "17", "1362699000013318565", "change_request", "Login fails", "Steps", []), now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PatchPony.Core.Workflows.TicketChangeType.Updated, result.Value!.ChangeType);
        Assert.Equal(PatchPony.Core.Workflows.TicketChannel.ChangeRequest, result.Value.Channel);
    }
}