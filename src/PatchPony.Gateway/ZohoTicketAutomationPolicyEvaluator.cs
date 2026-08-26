using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoTicketAutomationPolicy
{
    public string ProjectManifestId { get; init; } = string.Empty;
    public bool AllowConfigFix { get; init; }
    public IReadOnlyList<TicketPlanEvidenceKind> RequiredEvidence { get; init; } = [];
}

public sealed class ZohoTicketAutomationPolicyEvaluator(IConfiguration configuration)
{
    public Result<TicketAutomationDecision> Decide(TicketTriageOutput triage, TicketPlanOutput? plan)
    {
        ArgumentNullException.ThrowIfNull(triage);
        if (triage.Disposition == TicketTriageDisposition.NeedsInformation) return Result<TicketAutomationDecision>.Success(TicketAutomationDecision.NeedsInformation());
        if (plan is null) return Result<TicketAutomationDecision>.Success(TicketAutomationDecision.ManualInvestigation());
        var policies = configuration.GetSection($"{ZohoWebhookOptions.SectionName}:AutomationPolicies").Get<ZohoTicketAutomationPolicy[]>() ?? [];
        var matches = policies.Where(policy => string.Equals(policy.ProjectManifestId, triage.ProjectManifestId, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || !IsValid(matches[0])) return Result<TicketAutomationDecision>.Success(TicketAutomationDecision.ManualInvestigation());
        return TicketAutomationDecision.Create(plan, matches[0].AllowConfigFix, matches[0].RequiredEvidence.ToHashSet());
    }
    private static bool IsValid(ZohoTicketAutomationPolicy policy) => !string.IsNullOrWhiteSpace(policy.ProjectManifestId) && policy.ProjectManifestId.Length <= 63 && policy.ProjectManifestId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_') && policy.RequiredEvidence.Distinct().Count() == policy.RequiredEvidence.Count;
}