using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Publication;

public sealed record GitPublicationTemplate(string BranchName, string CommitMessage)
{
    private static readonly Regex ManifestIdPattern = new("^[a-z0-9][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>Builds publication text only from server-owned project, job and session identifiers.</summary>
    public static Result<GitPublicationTemplate> Create(Project project, Job job, Session session)
    {
        if (project is null || job is null || session is null || project.Id != job.ProjectId || project.Id != session.ProjectId || job.Id != session.JobId ||
            !ManifestIdPattern.IsMatch(project.ManifestId))
        {
            return Result<GitPublicationTemplate>.Failure(DomainError.Validation("Matching server-owned project, job and session data are required for publication."));
        }

        var branch = SessionNaming.For(session.Id).BranchName;
        var message = $"patchpony({project.ManifestId}): apply validated session changes" +
            $"\n\nPatchPony-Job: {job.Id.Value:N}" +
            $"\nPatchPony-Session: {session.Id.Value:N}";
        return Result<GitPublicationTemplate>.Success(new GitPublicationTemplate(branch, message));
    }
}