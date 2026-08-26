# Knowledge publication workflow

A knowledge change uses the same isolated session and Git controls as other PatchPony publication work. It is implemented by `KnowledgeSessionPublicationWorkflow` and deliberately has no merge operation.

1. An active, server-created session provides the only allowed worktree.
2. `KnowledgeSessionPatchService` verifies the source/project write policy, owner/reviewer rule, source hash, frontmatter and content policy.
3. It builds a bounded catalog of approved Markdown pages from that worktree, runs the link-impact analysis and atomically replaces exactly the approved existing Markdown file.
4. Push and merge-request approval gates are checked before the worktree is mutated.
5. The existing controlled Git services commit to the deterministic session bot branch, push only that branch and create or reuse one merge request.
6. Reviewers resolved from the affected knowledge area are requested from the Git provider.

A link-breaking proposal, stale content, an unapproved path, missing ownership, absent human approval, a remote mismatch or any Git/provider failure stops the workflow. PatchPony does not merge the merge request; a human remains responsible for final review and merge.