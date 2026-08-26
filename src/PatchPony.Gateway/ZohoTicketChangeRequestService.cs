using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

/// <summary>Gateway facade for controlled n8n-to-Core construction of a typed change request.</summary>
public sealed class ZohoTicketChangeRequestService
{
    public Result<TicketChangeRequest> Create(ProjectMappedTicket ticket, RequestedChangeKind kind, IReadOnlyList<RequestedChangeTarget> targets) => TicketChangeRequest.Create(ticket, kind, targets);
}