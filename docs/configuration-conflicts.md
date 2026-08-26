# Konfigurationskonflikte

I12.4 erkennt explizit konfigurierte, verbotene Kombinationen von zwei Sollwerten innerhalb eines einzelnen, bereits schlüssel- und typvalidierten Änderungsauftrags.

```text
PatchPony__ConfigurationConflictPolicies__Entries__0__ProjectManifestId=patchpony
PatchPony__ConfigurationConflictPolicies__Entries__0__Environment=Production
PatchPony__ConfigurationConflictPolicies__Entries__0__LeftKey=config/auth-mode
PatchPony__ConfigurationConflictPolicies__Entries__0__LeftValue=disabled
PatchPony__ConfigurationConflictPolicies__Entries__0__RightKey=config/public-access
PatchPony__ConfigurationConflictPolicies__Entries__0__RightValue=true
```

Treffen beide Werte gleichzeitig zu, wird der Auftrag mit `config.values.conflict` abgelehnt. Fehlerantworten nennen weder Schlüssel noch Werte. Ungültige oder doppelte Regeln werden als Serverfehlkonfiguration behandelt.

Die Prüfung setzt die erfolgreiche I12.3-Allowlist-Validierung voraus und betrachtet nur Werte des vorgeschlagenen Änderungsauftrags. Abgleich mit dem aktuellen Konfigurationsbestand und mehrstufige Abhängigkeiten bleiben separate Erweiterungen; I12.4 führt keinerlei Patch oder Veröffentlichung aus.