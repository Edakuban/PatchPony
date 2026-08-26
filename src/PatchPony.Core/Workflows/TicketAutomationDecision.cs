using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum TicketWorkflowOutcome { NeedsInformation, PlanOnly, ChangeProposalReady, ManualInvestigationRequired }

public sealed record TicketAutomationDecision(TicketWorkflowOutcome Outcome, string ReasonCode)
{
    public static Result<TicketAutomationDecision> Create(TicketPlanOutput plan, bool allowConfigFix, IReadOnlySet<TicketPlanEvidenceKind> requiredEvidence)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(requiredEvidence);
        if (plan.Evidence.Count == 0 || plan.Steps.Count != 3) return Result<TicketAutomationDecision>.Failure(new DomainError("ticket.automation.invalid", "The ticket plan is invalid."));
        var kinds = plan.Evidence.Select(evidence => evidence.Kind).ToHashSet();
        if (kinds.Contains(TicketPlanEvidenceKind.Source)) return Result<TicketAutomationDecision>.Success(new TicketAutomationDecision(TicketWorkflowOutcome.PlanOnly, "automation.source_change_not_allowed"));
        if (!allowConfigFix) return Result<TicketAutomationDecision>.Success(new TicketAutomationDecision(TicketWorkflowOutcome.PlanOnly, "automation.config_fix_not_allowed"));
        if (!kinds.Contains(TicketPlanEvidenceKind.Config) || !requiredEvidence.IsSubsetOf(kinds)) return Result<TicketAutomationDecision>.Success(new TicketAutomationDecision(TicketWorkflowOutcome.PlanOnly, "automation.required_evidence_missing"));
        return Result<TicketAutomationDecision>.Success(new TicketAutomationDecision(TicketWorkflowOutcome.ChangeProposalReady, "automation.config_only_allowed"));
    }
    public static TicketAutomationDecision PlanOnly(string reasonCode) => new(TicketWorkflowOutcome.PlanOnly, reasonCode);
    public static TicketAutomationDecision NeedsInformation() => new(TicketWorkflowOutcome.NeedsInformation, "automation.ticket_incomplete");
    public static TicketAutomationDecision ManualInvestigation() => new(TicketWorkflowOutcome.ManualInvestigationRequired, "automation.policy_unconfigured");
}