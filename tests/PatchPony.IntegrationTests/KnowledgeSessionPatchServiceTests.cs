using System.Text;
using PatchPony.Core.Jobs;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Knowledge;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeSessionPatchServiceTests
{
    [Fact]
    public void Apply_WritesOnlyAValidatedProposalInsideTheActiveSessionWorktree()
    {
        var root = CreateRoot(out var session, out var access, out var ownership, out var layout);
        try
        {
            Write(layout.WorktreeRoot, "docs/guide.md", "---\ntitle: Before\n---\n# Guide\nOld\n");
            var source = File.ReadAllText(Path.Combine(layout.WorktreeRoot, "docs", "guide.md"));
            var request = new KnowledgePatchRequest("docs/guide.md", KnowledgePatchService.Sha256(source), [new SetKnowledgeFrontmatterField("title", "After"), new AppendKnowledgeMarkdown("More")]);

            var result = new KnowledgeSessionPatchService(new SessionWorkspaceLayoutResolver(root)).Apply(session, access, ownership, request);

            Assert.True(result.IsSuccess);
            Assert.Equal(["owner"], result.Value!.Ownership.Owners);
            var persisted = File.ReadAllText(Path.Combine(layout.WorktreeRoot, "docs", "guide.md"));
            Assert.Contains("title: \"After\"", persisted, StringComparison.Ordinal);
            Assert.Contains("More", persisted, StringComparison.Ordinal);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Apply_PreservesExistingHeadingFragmentsWhenReplacingASection()
    {
        var root = CreateRoot(out var session, out var access, out var ownership, out var layout);
        try
        {
            Write(layout.WorktreeRoot, "docs/guide.md", "# Install\nOld\n");
            Write(layout.WorktreeRoot, "docs/index.md", "[[guide#install]]\n");
            var source = File.ReadAllText(Path.Combine(layout.WorktreeRoot, "docs", "guide.md"));
            var request = new KnowledgePatchRequest("docs/guide.md", KnowledgePatchService.Sha256(source), [new ReplaceKnowledgeMarkdownSection("# Install", "New")]);

            var result = new KnowledgeSessionPatchService(new SessionWorkspaceLayoutResolver(root)).Apply(session, access, ownership, request);

            Assert.True(result.IsSuccess);
            Assert.Contains("# Install\nNew", File.ReadAllText(Path.Combine(layout.WorktreeRoot, "docs", "guide.md")), StringComparison.Ordinal);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static string CreateRoot(out Session session, out KnowledgeSourceProjectAccess access, out KnowledgeAreaOwnershipPolicy ownership, out SessionWorkspaceLayout layout)
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        var project = ProjectId.New();
        var now = DateTimeOffset.UtcNow;
        session = Session.Create(SessionId.New(), project, JobId.New(), now, now.AddHours(1)).Value!;
        session = session.Transition(SessionStatus.Provisioning, now).Value!.Transition(SessionStatus.Active, now.AddMinutes(1)).Value!;
        layout = new SessionWorkspaceLayoutResolver(root).Resolve(session.Id);
        Directory.CreateDirectory(layout.WorktreeRoot);
        access = KnowledgeSourceProjectAccess.Create(KnowledgeSourceId.New(), project, ["docs/**"], ["docs/**"]).Value!;
        ownership = new KnowledgeAreaOwnershipPolicy([new KnowledgeAreaOwnershipRule(project.Value.ToString("N"), "docs/**", ["owner"], ["reviewer"])]);
        return root;
    }

    private static void Write(string root, string relative, string content)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    [Fact]
    public void Apply_RecordsContentFreeLinkImpactOwnershipAndPatchAudit()
    {
        var root = CreateRoot(out var session, out var access, out var ownership, out var layout);
        try
        {
            Write(layout.WorktreeRoot, "docs/guide.md", "# Guide\nOld\n");
            var source = File.ReadAllText(Path.Combine(layout.WorktreeRoot, "docs", "guide.md"));
            var request = new KnowledgePatchRequest("docs/guide.md", KnowledgePatchService.Sha256(source), [new AppendKnowledgeMarkdown("More")]);
            var audit = new RecordingKnowledgeAudit();

            var result = new KnowledgeSessionPatchService(new SessionWorkspaceLayoutResolver(root), audit: audit).Apply(session, access, ownership, request);

            Assert.True(result.IsSuccess);
            Assert.Equal(["link-impact", "ownership-resolved", "patch-applied"], audit.Events.Select(item => item.Operation));
            Assert.All(audit.Events, item => Assert.NotEqual("docs/guide.md", item.PathFingerprint));
            Assert.All(audit.Events, item => Assert.DoesNotContain("owner", string.Join('|', item.OwnerFingerprints ?? []), StringComparison.Ordinal));
            var impact = audit.Events[0];
            Assert.Equal("analyzed", impact.Outcome);
            Assert.Equal(0, impact.BrokenFragmentBacklinks);
            Assert.Single(audit.Events[1].OwnerFingerprints!);
            Assert.Single(audit.Events[1].ReviewerFingerprints!);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class RecordingKnowledgeAudit : IKnowledgeAuditSink
    {
        public List<KnowledgeAuditEvent> Events { get; } = [];

        public void Record(KnowledgeAuditEvent auditEvent) => Events.Add(auditEvent);

        public IReadOnlyList<KnowledgeAuditEvent> GetRecent(int maximumCount) => Events.Take(maximumCount).ToArray();
    }}
