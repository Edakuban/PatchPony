using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public sealed record TicketInformationRequest(string TicketExternalId, string TicketRevision, string ProjectManifestId, string IdempotencyKey, IReadOnlyList<string> Questions)
{
    public static Result<TicketInformationRequest> Create(TicketTriageOutput triage)
    {
        ArgumentNullException.ThrowIfNull(triage);
        if (triage.Disposition != TicketTriageDisposition.NeedsInformation || triage.IsComplete || triage.MissingCriteria.Count is < 1 or > 5)
            return Result<TicketInformationRequest>.Failure(new DomainError("ticket.information_request.invalid", "The triage does not require an information request."));
        var questions = new List<string>();
        foreach (var criterion in triage.MissingCriteria)
        {
            var question = criterion switch
            {
                "description.minimum_length" => "Bitte ergänze eine ausreichend detaillierte Fehlerbeschreibung mit Schritten zur Reproduktion.",
                "attachments.minimum_count" => "Bitte füge die in der Projektpolicy benötigten, zulässigen Diagnoseanhänge hinzu.",
                _ when criterion.StartsWith("description.required_phrase:", StringComparison.Ordinal) => $"Bitte ergänze in der Fehlerbeschreibung Angaben zu: {criterion["description.required_phrase:".Length..]}.",
                _ => null
            };
            if (question is null || question.Length > 256 || question.Any(char.IsControl)) return Result<TicketInformationRequest>.Failure(new DomainError("ticket.information_request.invalid", "The missing criterion cannot be rendered safely."));
            questions.Add(question);
        }
        return Result<TicketInformationRequest>.Success(new TicketInformationRequest(triage.TicketExternalId, triage.TicketRevision, triage.ProjectManifestId, triage.IdempotencyKey, questions));
    }
}