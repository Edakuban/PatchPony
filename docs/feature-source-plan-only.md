# Feature-Requests mit Source-Änderungen

I12.7 setzt eine vorrangige, enge Policy: Ein `feature_request` mit Änderungsart `SourceCode` erhält immer

```text
outcome: plan_only
reason: automation.feature_source_plan_only
```

Die Entscheidung hängt weder von einer Projekt-Allowlist, Evidenz noch von n8n- oder Modellangaben ab. Sie verhindert damit, dass ein Feature-Request mit Source-Änderung in den Config-only-Automatisierungspfad fällt. Der bestehende, allgemeine Source-Evidenzschutz bleibt zusätzlich aktiv.

Die Policy betrifft ausschließlich diese Kombination. Config-Change-Requests und andere Änderungsarten werden nicht von ihr entschieden, sondern durch die I12.2–I12.6-Policies bzw. spätere Workflows behandelt. `plan_only` erstellt keinen Patch, keinen Branch, keinen Push und keinen Merge Request.