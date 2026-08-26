using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoTicketCompletenessPolicy
{
    public string ProjectManifestId { get; init; } = string.Empty;
    public int MinimumDescriptionLength { get; init; } = 40;
    public int MinimumAttachmentCount { get; init; }
    public IReadOnlyList<string> RequiredDescriptionPhrases { get; init; } = [];
}

/// <summary>Server-configured project completeness policy; it never derives criteria from ticket text.</summary>
public sealed class ZohoTicketCompletenessEvaluator(IConfiguration configuration)
{
    public Result<TicketCompletenessAssessment> Evaluate(ProjectMappedTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var policies = configuration.GetSection($"{ZohoWebhookOptions.SectionName}:CompletenessPolicies").Get<ZohoTicketCompletenessPolicy[]>() ?? [];
        var matches = policies.Where(policy => string.Equals(policy.ProjectManifestId, ticket.ProjectManifestId, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || !IsValid(matches.Single())) return Result<TicketCompletenessAssessment>.Failure(new DomainError("ticket.completeness.unconfigured", "No valid completeness policy is configured for the project."));

        var policy = matches[0];
        var missing = new List<string>();
        if (ticket.Ticket.Description.Length < policy.MinimumDescriptionLength) missing.Add("description.minimum_length");
        if (ticket.Ticket.Attachments.Count < policy.MinimumAttachmentCount) missing.Add("attachments.minimum_count");
        foreach (var phrase in policy.RequiredDescriptionPhrases)
        {
            if (ticket.Ticket.Description.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) < 0) missing.Add("description.required_phrase:" + phrase);
        }
        return Result<TicketCompletenessAssessment>.Success(missing.Count == 0 ? TicketCompletenessAssessment.Complete() : TicketCompletenessAssessment.Incomplete(missing));
    }

    private static bool IsValid(ZohoTicketCompletenessPolicy policy) =>
        !string.IsNullOrWhiteSpace(policy.ProjectManifestId) && policy.ProjectManifestId.Length <= 63 && policy.ProjectManifestId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_') &&
        policy.MinimumDescriptionLength is >= 1 and <= 16 * 1024 && policy.MinimumAttachmentCount is >= 0 and <= 5 && policy.RequiredDescriptionPhrases.Count <= 5 && policy.RequiredDescriptionPhrases.All(phrase => !string.IsNullOrWhiteSpace(phrase) && phrase.Length <= 80 && phrase.All(character => !char.IsControl(character)));
}