using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoInformationRequestService
{
    public Result<TicketInformationRequest> Create(TicketTriageOutput triage) => TicketInformationRequest.Create(triage);
}