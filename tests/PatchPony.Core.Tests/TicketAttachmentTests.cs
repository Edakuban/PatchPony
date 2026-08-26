using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketAttachmentTests
{
    [Fact]
    public void Attachment_AllowsBoundedTextMetadata()
    {
        var result = NormalizedTicketAttachment.Create("ATT-1", "error.log", "text/plain", 1024);
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("evidence.zip", "application/zip", 1024)]
    [InlineData("image.png", "image/png", 1024)]
    [InlineData("huge.log", "text/plain", 2097153)]
    [InlineData("../secret.log", "text/plain", 1024)]
    public void Attachment_RejectsArchivesUnsupportedTypesAndUnsafeMetadata(string fileName, string contentType, long sizeBytes)
    {
        Assert.False(NormalizedTicketAttachment.Create("ATT-1", fileName, contentType, sizeBytes).IsSuccess);
    }

    [Fact]
    public void Ticket_RejectsMoreThanFiveAttachmentsOrMoreThanFiveMiBTotal()
    {
        var attachment = NormalizedTicketAttachment.Create("ATT-1", "error.log", "text/plain", 1024 * 1024).Value!;
        var tooMany = Enumerable.Range(1, 6).Select(index => attachment with { ExternalId = $"ATT-{index}" }).ToArray();
        var tooLarge = Enumerable.Range(1, 5).Select(index => attachment with { ExternalId = $"ATT-{index}", SizeBytes = 2 * 1024 * 1024 }).ToArray();

        Assert.False(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow, tooMany).IsSuccess);
        Assert.False(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow, tooLarge).IsSuccess);
    }
}