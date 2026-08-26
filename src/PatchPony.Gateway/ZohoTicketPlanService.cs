using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoTicketPlanService
{
    public Result<TicketPlanOutput> Create(TicketTriageOutput triage, IReadOnlyList<TicketPlanEvidence> evidence) => TicketPlanOutput.Create(triage, evidence);
}