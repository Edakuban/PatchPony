using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

/// <summary>Composes the server-derived project and completeness results into the workflow triage contract.</summary>
public sealed class ZohoTicketTriageService
{
    public Result<TicketTriageOutput> Create(ProjectMappedTicket ticket, string idempotencyKey, TicketCompletenessAssessment completeness) => TicketTriageOutput.Create(ticket, idempotencyKey, completeness);
}