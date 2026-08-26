# Config-only-Automatisierungs-Allowlist

I12.6 ist eine zusätzliche, explizite Projektgrenze. Ein bereits validierter Config-Änderungsauftrag darf nur dann in einen automatisierten Config-only-Handoff, wenn genau eine serverseitige Projektregel dies erlaubt.

```text
PatchPony__ConfigurationAutomationProjects__Entries__0__ProjectManifestId=patchpony
PatchPony__ConfigurationAutomationProjects__Entries__0__AllowConfigOnlyAutomation=true
```

Für nicht konfigurierte Projekte gibt es keinen Default. Eine fehlende oder mehrdeutige Regel liefert `config.automation_policy.unconfigured`; eine explizit deaktivierte Regel liefert `config.automation.not_allowed`. Die Gate-Prüfung ergänzt I11.11, I12.2 bis I12.5 – sie ersetzt keine Schlüsselprüfung, Konflikterkennung, Reviewer oder Approval.

Die Regel betrifft ausschließlich `Configuration`-Änderungsaufträge. Source-Code, Datenbankmigrationen und Secrets erhalten über diesen Pfad keine Automatisierungsfreigabe.