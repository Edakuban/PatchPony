using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

/// <summary>Provider-neutral ticket after server-side project selection.</summary>
public sealed record ProjectMappedTicket(NormalizedTicket Ticket, string ProjectManifestId)
{
    public static Result<ProjectMappedTicket> Create(NormalizedTicket ticket, string projectManifestId)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        if (string.IsNullOrWhiteSpace(projectManifestId) || projectManifestId.Length > 63 || !projectManifestId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            return Result<ProjectMappedTicket>.Failure(new DomainError("ticket.project_mapping.invalid", "The configured project mapping is invalid."));
        return Result<ProjectMappedTicket>.Success(new ProjectMappedTicket(ticket, projectManifestId));
    }
}