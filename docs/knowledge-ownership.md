# Knowledge ownership and review

Knowledge changes must be assigned by a server-owned area rule before they enter a publication workflow. A rule has a project ID, a vault-relative glob, one or more owners and one or more reviewers.

```text
PATCHPONY_KNOWLEDGEOWNERSHIP__ENTRIES__0__PROJECTID=demo-project
PATCHPONY_KNOWLEDGEOWNERSHIP__ENTRIES__0__PATHPATTERN=docs/security/**
PATCHPONY_KNOWLEDGEOWNERSHIP__ENTRIES__0__OWNERS__0=security-owner
PATCHPONY_KNOWLEDGEOWNERSHIP__ENTRIES__0__REVIEWERS__0=security-reviewer
```

Rules are never supplied by an API caller. A path must match exactly one most-specific rule. Missing rules and ties at the same specificity fail closed. Each rule needs at least one owner and one reviewer; a person cannot occupy both roles in the same rule, so review is independent by construction.

This policy resolves accountability only. It does not yet publish a change or request a Git review; that controlled integration is part of I13.10.