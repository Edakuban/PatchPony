using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoProjectMapping
{
    public string ZohoProjectId { get; init; } = string.Empty;
    public string ProjectManifestId { get; init; } = string.Empty;
}

/// <summary>Server-configured, fail-closed routing from Zoho project identifiers to project manifest IDs.</summary>
public sealed class ZohoTicketProjectResolver(IConfiguration configuration)
{
    public Result<ProjectMappedTicket> Resolve(NormalizedTicket ticket, string zohoProjectId)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var mappings = configuration.GetSection($"{ZohoWebhookOptions.SectionName}:ProjectMappings").Get<ZohoProjectMapping[]>() ?? [];
        if (mappings.Length == 0 || mappings.Any(mapping => !IsValid(mapping)))
            return Result<ProjectMappedTicket>.Failure(new DomainError("ticket.project_mapping.unconfigured", "No valid Zoho project mapping is configured."));

        var matches = mappings.Where(mapping => string.Equals(mapping.ZohoProjectId, zohoProjectId, StringComparison.Ordinal)).ToArray();
        if (matches.Length == 0)
            return Result<ProjectMappedTicket>.Failure(new DomainError("ticket.project_mapping.unmapped", "The ticket is not mapped to a project."));
        if (matches.Length > 1)
            return Result<ProjectMappedTicket>.Failure(new DomainError("ticket.project_mapping.ambiguous", "The ticket project mapping is ambiguous."));

        return ProjectMappedTicket.Create(ticket, matches[0].ProjectManifestId);
    }

    private static bool IsValid(ZohoProjectMapping mapping) =>
        !string.IsNullOrWhiteSpace(mapping.ZohoProjectId) && mapping.ZohoProjectId.Length <= 64 && mapping.ZohoProjectId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.') &&
        !string.IsNullOrWhiteSpace(mapping.ProjectManifestId) && mapping.ProjectManifestId.Length <= 63 && mapping.ProjectManifestId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}