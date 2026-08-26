# Reviewer und Freigaben für Config-Änderungen

I12.5 kombiniert die feste Environment-Matrix mit einer serverseitigen Reviewer-Zuordnung pro lokaler `ProjectManifestId` und Umgebung.

```text
PatchPony__ConfigurationReviewPolicies__Entries__0__ProjectManifestId=patchpony
PatchPony__ConfigurationReviewPolicies__Entries__0__Environment=Staging
PatchPony__ConfigurationReviewPolicies__Entries__0__Reviewers__0=release-owner
```

Development benötigt keine Reviewer-Regel. Staging und Production benötigen genau eine Regel mit ein bis sechzehn gültigen GitHub-Benutzernamen. Doppelte, ungültige oder fehlende Policies werden fail-closed abgelehnt. Für Production transportiert die aufgelöste Anforderung zusätzlich `ExplicitHumanApprovalRequiredBeforePublication=true`.

I12.5 bestimmt nur die Anforderung und führt noch keinen GitHub-Reviewer-Aufruf oder Approval-Write aus. Diese Daten werden im kontrollierten Publication-Handoff mit den bestehenden Git- und Approval-Komponenten verbunden; n8n, Modell und Tickettext können sie nicht überschreiben.