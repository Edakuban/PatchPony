using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum TicketPlanEvidenceKind { Skill, Source, Config, Knowledge }

public sealed record TicketPlanEvidence(TicketPlanEvidenceKind Kind, string Reference)
{
    public static Result<TicketPlanEvidence> Create(TicketPlanEvidenceKind kind, string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 512 || reference.StartsWith('/') || reference.Contains((char)92) || reference.Contains("..", StringComparison.Ordinal) || reference.Any(char.IsControl)) return Result<TicketPlanEvidence>.Failure(new DomainError("ticket.plan.evidence_invalid", "The plan evidence reference is invalid."));
        return Result<TicketPlanEvidence>.Success(new TicketPlanEvidence(kind, reference));
    }
}

public sealed record TicketPlanStep(string Id, string Title, IReadOnlyList<TicketPlanEvidence> Evidence);

public sealed record TicketPlanOutput(string TicketExternalId, string TicketRevision, string ProjectManifestId, string IdempotencyKey, IReadOnlyList<TicketPlanEvidence> Evidence, IReadOnlyList<TicketPlanStep> Steps)
{
    public static Result<TicketPlanOutput> Create(TicketTriageOutput triage, IReadOnlyList<TicketPlanEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(triage);
        if (triage.Disposition != TicketTriageDisposition.ReadyForPlanning || !triage.IsComplete || evidence is null || evidence.Count is < 1 or > 16 || evidence.Select(item => (item.Kind, item.Reference)).Distinct().Count() != evidence.Count) return Result<TicketPlanOutput>.Failure(new DomainError("ticket.plan.invalid", "The ticket plan input is invalid."));
        var ordered = evidence.OrderBy(item => item.Kind).ThenBy(item => item.Reference, StringComparer.Ordinal).ToArray();
        var steps = new[]
        {
            new TicketPlanStep("review-evidence", "Freigegebene Projektquellen und Vorgaben prüfen", ordered),
            new TicketPlanStep("propose-change", "Änderung als überprüfbaren Vorschlag ausarbeiten", ordered.Where(item => item.Kind is TicketPlanEvidenceKind.Source or TicketPlanEvidenceKind.Config).ToArray()),
            new TicketPlanStep("validate-proposal", "Vorschlag gegen registrierte Prüfungen und Reviewregeln validieren", ordered.Where(item => item.Kind is TicketPlanEvidenceKind.Skill or TicketPlanEvidenceKind.Knowledge).ToArray())
        };
        return Result<TicketPlanOutput>.Success(new TicketPlanOutput(triage.TicketExternalId, triage.TicketRevision, triage.ProjectManifestId, triage.IdempotencyKey, ordered, steps));
    }
}