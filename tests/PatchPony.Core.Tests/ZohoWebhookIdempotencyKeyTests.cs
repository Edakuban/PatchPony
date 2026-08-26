using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ZohoWebhookIdempotencyKeyTests
{
    [Fact]
    public void Create_IsStableForTheSameTicketRevisionAndEvent()
    {
        var first = ZohoWebhookIdempotencyKey.Create("task.updated", "ZOHO-42", "17");
        var replay = ZohoWebhookIdempotencyKey.Create("task.updated", "ZOHO-42", "17");
        var changedRevision = ZohoWebhookIdempotencyKey.Create("task.updated", "ZOHO-42", "18");
        var changedEvent = ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17");

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value, replay.Value);
        Assert.NotEqual(first.Value, changedRevision.Value);
        Assert.NotEqual(first.Value, changedEvent.Value);
        Assert.Matches("^zoho:v1:[0-9a-f]{64}$", first.Value!);
    }

    [Theory]
    [InlineData("other.event", "ZOHO-42", "17")]
    [InlineData("task.created", "ZOHO 42", "17")]
    [InlineData("task.created", "ZOHO-42", "rev 17")]
    public void Create_RejectsInvalidIdentityParts(string eventType, string ticketId, string revision)
    {
        Assert.False(ZohoWebhookIdempotencyKey.Create(eventType, ticketId, revision).IsSuccess);
    }
}