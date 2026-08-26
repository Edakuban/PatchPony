# Allowlist für Config-Schlüssel

I12.3 akzeptiert Config-Änderungen nur für exakt serverseitig konfigurierte Schlüssel. Jede Regel wird nach lokaler `ProjectManifestId` und Environment ausgewählt und definiert einen Schlüssel, einen Typ und – sofern zutreffend – seinen Wertebereich.

```text
PatchPony__ConfigurationKeyPolicies__Entries__0__ProjectManifestId=patchpony
PatchPony__ConfigurationKeyPolicies__Entries__0__Environment=Development
PatchPony__ConfigurationKeyPolicies__Entries__0__Key=config/retries
PatchPony__ConfigurationKeyPolicies__Entries__0__ValueType=Integer
PatchPony__ConfigurationKeyPolicies__Entries__0__Minimum=0
PatchPony__ConfigurationKeyPolicies__Entries__0__Maximum=5
```

Verfügbare Typen sind `Boolean` (`true` oder `false`), `Integer` (inklusive Minimum/Maximum), `String` (optional `MaximumLength`) und `Enum` (exakte `AllowedValues`). Leere, ungültige oder doppelte Policies sowie unbekannte Schlüssel stoppen fail-closed. Werte werden nicht im Validierungsergebnis zurückgegeben.

Der Katalog hat keine Defaults. Secrets stehen in keiner Allowlist und können daher nicht über einen Config-Änderungsauftrag zugelassen werden. I12.3 entscheidet weder über Widersprüche zwischen Schlüsseln noch über Reviewer oder Veröffentlichung.