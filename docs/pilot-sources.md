# Pilotquellen für I6

Für die erste Quellen-Integration sind genau zwei lokale, read-only Checkouts vorgesehen:

| Projekt-ID | Quelle | Read-Policy |
|---|---|---|
| `patchpony` | aktueller PatchPony-Workspace | `patchpony.yaml` |
| `vocavid` | flache lokale Kopie von `Edakuban/VocaVid` unter `pilot-sources/VocaVid` | `vocavid.yaml` |

Die Manifeste liegen unter `integrations/pilot-projects/` und erlauben ausschließlich Quellcode, Tests, Dokumentation und klar benannte Projektdateien. Git-Metadaten, `.env`-Dateien, VocaVid-Asset-/Datenbankordner sowie alle Schreibpfade sind ausgeschlossen.

Die Pilotkopie von VocaVid ist über `.gitignore` vollständig aus dem PatchPony-Repository ausgeschlossen. Sie dient nur dazu, die kommende Gateway-Quelle-/Fundstellenkette zu testen; echte Projekt-Repositories werden erst danach angebunden.