# I12.9 – Historische Change-Requests evaluieren

[`i12-change-requests.json`](../evaluations/i12-change-requests.json) ist der versionierte, datensparsame Durchführungssatz für die Change-Request-Policy. Er enthält neun repräsentative Kategorien, aber bewusst keine echten Tickettexte, Attachments, Benutzerkennungen oder Secrets.

## Durchführung

1. Mindestens zehn abgeschlossene Zoho-Tasks mit `channel=change_request` auswählen und außerhalb des Repositories anonymisieren.
2. Pro Fall nur ID-Ersatz, Kanal, Änderungsart, Zielumgebung, Zielschlüsselklasse und erwartetes Ergebnis in den Satz übernehmen. Keine Beschreibungen oder Werte kopieren.
3. Den Fall über den deaktivierten n8n-Intake und die lokalen I12.2–I12.8-Policies rekapitulieren; keine reale Config, kein Branch und kein Merge Request.
4. Das beobachtete Ergebnis als `pass`, `partial` oder `fail` in einem separaten, zugriffsbeschränkten Ergebnisdokument festhalten.

Mindestens 90 % der Fälle müssen `pass` erhalten. Jede unfreigegebene Automatisierung, angenommener Secret-Sollwert, Source-Feature ohne `plan_only` oder Policy-Bypass ist unabhängig von der Quote ein Blocker.

Der aktuelle Status `pending-historical-export` ist absichtlich: Für eine echte historische Auswertung fehlen noch anonymisierte Fälle aus Zoho. Das Fixture und die Testabdeckung sind vorbereitet, I12.9 wird erst nach dieser Durchführung als fachlich abgeschlossen markiert.