using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class NormalizedTicketTests
{
    [Fact]
    public void Create_UsesOnlyProviderNeutralFields()
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var ticket = NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Updated, "ZOHO-42", "17", "Login fails", "Steps\n1. Open app", receivedAt);

        Assert.True(ticket.IsSuccess);
        Assert.Equal(ExternalTicketProvider.Zoho, ticket.Value!.Provider);
        Assert.Equal(TicketChangeType.Updated, ticket.Value.ChangeType);
        Assert.Equal("ZOHO-42", ticket.Value.ExternalId);
        Assert.Equal(receivedAt, ticket.Value.ReceivedAt);
    }

    [Theory]
    [InlineData("bad id", "17", "Title", "Description")]
    [InlineData("ZOHO-42", "17", "Bad\u0000title", "Description")]
    [InlineData("ZOHO-42", "17", "Title", "")]
    public void Create_RejectsInvalidOrUnsafeTicketFields(string id, string revision, string title, string description)
    {
        Assert.False(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, id, revision, title, description, DateTimeOffset.UtcNow).IsSuccess);
    }
}