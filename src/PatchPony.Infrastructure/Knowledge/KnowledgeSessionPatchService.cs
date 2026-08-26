using System.Text;
using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Paths;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.Infrastructure.Knowledge;

public sealed record KnowledgeSessionPatchResult(KnowledgePatchProposal Proposal, KnowledgeAreaReviewRequirement Ownership, KnowledgeChangeImpact Impact);

/// <summary>Applies one validated proposal atomically to an existing active session worktree. It never commits, pushes or merges.</summary>
public sealed class KnowledgeSessionPatchService(SessionWorkspaceLayoutResolver layouts, KnowledgePatchService? patches = null, KnowledgeLinkImpactAnalyzer? impacts = null, IKnowledgeAuditSink? audit = null)
{
    private static readonly KnowledgeVaultContentPolicy ContentPolicy = new();
    private readonly KnowledgePatchService patches = patches ?? new KnowledgePatchService();
    private readonly KnowledgeLinkImpactAnalyzer impacts = impacts ?? new KnowledgeLinkImpactAnalyzer();
    private readonly IKnowledgeAuditSink audit = audit ?? NullKnowledgeAuditSink.Instance;

    public Result<KnowledgeSessionPatchResult> Apply(Session session, KnowledgeSourceProjectAccess access, KnowledgeAreaOwnershipPolicy ownership, KnowledgePatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(request);
        if (session.Status != SessionStatus.Active || access.ProjectId != session.ProjectId) return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.session.invalid_state", "Knowledge changes require a matching active session."));
        var layout = layouts.Resolve(session.Id);
        if (!Directory.Exists(layout.WorktreeRoot) || !SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, layout.WorktreeRoot, true)) return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.session.unavailable", "The controlled session worktree is unavailable."));
        var write = access.Authorize(request.Path, KnowledgePathAccess.Write);
        if (!write.IsSuccess) return Result<KnowledgeSessionPatchResult>.Failure(write.Error);
        var owners = ownership.Resolve(session.ProjectId.Value.ToString("N"), request.Path);
        if (!owners.IsSuccess) return Result<KnowledgeSessionPatchResult>.Failure(owners.Error);

        var resolver = new ProjectPathResolver(layout.WorktreeRoot);
        var target = resolver.Resolve(request.Path);
        if (!target.IsSuccess || !File.Exists(target.Value!.FullPath) || IsReparsePoint(target.Value.FullPath)) return Result<KnowledgeSessionPatchResult>.Failure(target.IsSuccess ? new DomainError("knowledge.session.not_found", "The requested Markdown page is unavailable in the session worktree.") : target.Error);
        if (new FileInfo(target.Value.FullPath).Length > KnowledgePatchService.MaximumDocumentBytes) return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.patch.too_large", "The Markdown document exceeds the patch size limit."));

        try
        {
            var current = File.ReadAllText(target.Value.FullPath, new UTF8Encoding(false, true));
            var catalog = BuildCatalog(layout.WorktreeRoot, resolver, access);
            if (!catalog.IsSuccess) return Result<KnowledgeSessionPatchResult>.Failure(catalog.Error);
            var proposal = patches.Propose(current, request, cancellationToken);
            if (!proposal.IsSuccess) return Result<KnowledgeSessionPatchResult>.Failure(proposal.Error);
            var impact = impacts.AnalyzeChange(catalog.Value!, request.Path, proposal.Value!.Content);
            if (!impact.IsSuccess) return Result<KnowledgeSessionPatchResult>.Failure(impact.Error);
            RecordImpact(session, request.Path, impact.Value!);
            if (impact.Value!.BrokenFragmentBacklinks.Count > 0) return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.patch.broken_links", "The proposed change would break internal heading links."));

            var temporary = Path.Combine(Path.GetDirectoryName(target.Value.FullPath)!, $".patchpony-{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(temporary, proposal.Value.Content, new UTF8Encoding(false));
                File.Move(temporary, target.Value.FullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            RecordPublication(session, request.Path, proposal.Value!, owners.Value!);
            return Result<KnowledgeSessionPatchResult>.Success(new KnowledgeSessionPatchResult(proposal.Value, owners.Value!, impact.Value));
        }
        catch (DecoderFallbackException) { return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.session.invalid_encoding", "The session Markdown file is not valid UTF-8.")); }
        catch (IOException) { return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.session.unavailable", "The controlled session worktree is unavailable.")); }
        catch (UnauthorizedAccessException) { return Result<KnowledgeSessionPatchResult>.Failure(new DomainError("knowledge.session.unavailable", "The controlled session worktree is unavailable.")); }
    }

    private void RecordImpact(Session session, string path, KnowledgeChangeImpact impact) => audit.Record(new KnowledgeAuditEvent(
        DateTimeOffset.UtcNow,
        "session-" + session.Id.Value.ToString("N"),
        session.ProjectId.Value.ToString("N"),
        session.Id.Value.ToString("N"),
        "link-impact",
        impact.BrokenFragmentBacklinks.Count == 0 ? "analyzed" : "blocked",
        KnowledgeAuditFingerprint.ForValue(path),
        null,
        null,
        impact.AddedLinks.Count,
        impact.RemovedLinks.Count,
        impact.IncomingBacklinks.Count,
        impact.BrokenFragmentBacklinks.Count));

    private void RecordPublication(Session session, string path, KnowledgePatchProposal proposal, KnowledgeAreaReviewRequirement ownership)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var correlation = "session-" + session.Id.Value.ToString("N");
        var project = session.ProjectId.Value.ToString("N");
        var sessionReference = session.Id.Value.ToString("N");
        var pathFingerprint = KnowledgeAuditFingerprint.ForValue(path);
        audit.Record(new KnowledgeAuditEvent(
            occurredAt,
            correlation,
            project,
            sessionReference,
            "ownership-resolved",
            "applied",
            pathFingerprint,
            null,
            null,
            null,
            null,
            null,
            null,
            ownership.Owners.Select(KnowledgeAuditFingerprint.ForValue).ToArray(),
            ownership.Reviewers.Select(KnowledgeAuditFingerprint.ForValue).ToArray()));
        audit.Record(new KnowledgeAuditEvent(
            occurredAt,
            correlation,
            project,
            sessionReference,
            "patch-applied",
            "applied",
            pathFingerprint,
            proposal.Sha256,
            proposal.AppliedOperations.Count));
    }
    private static Result<IReadOnlyList<KnowledgeVaultPage>> BuildCatalog(string root, ProjectPathResolver resolver, KnowledgeSourceProjectAccess access)
    {
        try
        {
            var pages = new List<KnowledgeVaultPage>();
            foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
            {
                if (pages.Count == KnowledgeLinkImpactAnalyzer.MaximumPages) return Result<IReadOnlyList<KnowledgeVaultPage>>.Failure(new DomainError("knowledge.impact.catalog_invalid", "The session knowledge catalog exceeds the page limit."));
                if (IsReparsePoint(file)) continue;
                var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                var resolved = resolver.Resolve(relative);
                var readable = resolved.IsSuccess ? access.Authorize(relative, KnowledgePathAccess.Read) : Result.Failure(resolved.Error);
                if (!readable.IsSuccess) continue;
                if (new FileInfo(file).Length > KnowledgePatchService.MaximumDocumentBytes) continue;
                pages.Add(new KnowledgeVaultPage(relative, File.ReadAllText(file, new UTF8Encoding(false, true))));
            }
            return Result<IReadOnlyList<KnowledgeVaultPage>>.Success(pages);
        }
        catch (IOException) { return Result<IReadOnlyList<KnowledgeVaultPage>>.Failure(new DomainError("knowledge.session.unavailable", "The controlled session worktree is unavailable.")); }
        catch (UnauthorizedAccessException) { return Result<IReadOnlyList<KnowledgeVaultPage>>.Failure(new DomainError("knowledge.session.unavailable", "The controlled session worktree is unavailable.")); }
    }

    private static bool IsReparsePoint(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}