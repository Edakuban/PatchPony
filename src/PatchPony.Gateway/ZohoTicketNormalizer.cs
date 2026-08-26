using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoTicketNormalizer
{
    public Result<NormalizedTicket> Normalize(ZohoTaskWebhook webhook, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(webhook);
        var changeType = webhook.EventType switch { "task.created" => TicketChangeType.Created, "task.updated" => TicketChangeType.Updated, _ => (TicketChangeType?)null };
        var channel = webhook.Channel switch { "generic_task" => TicketChannel.GenericTask, "feature_request" => TicketChannel.FeatureRequest, "bug" => TicketChannel.Bug, "change_request" => TicketChannel.ChangeRequest, _ => (TicketChannel?)null };
        if (changeType is null || channel is null) return Result<NormalizedTicket>.Failure(new DomainError("ticket.normalization.invalid", "The Zoho task data is invalid."));
        var attachments = new List<NormalizedTicketAttachment>();
        foreach (var attachment in webhook.Attachments)
        {
            var normalized = NormalizedTicketAttachment.Create(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes);
            if (!normalized.IsSuccess) return Result<NormalizedTicket>.Failure(normalized.Error);
            attachments.Add(normalized.Value!);
        }
        return NormalizedTicket.Create(ExternalTicketProvider.Zoho, changeType.Value, webhook.TaskId, webhook.Revision, webhook.Title, webhook.Description, receivedAt, attachments, channel.Value);
    }
}