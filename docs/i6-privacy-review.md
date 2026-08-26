# I6.10 – Technische Datenschutzprüfung des übertragenen Kontexts

Diese Prüfung beschreibt den implementierten Read-only-Pilot, keine Rechtsberatung und keine formale Datenschutzfreigabe. Sie orientiert sich am Grundsatz der Zweckbindung und Datenminimierung; die DSGVO verlangt einen angemessenen, verhältnismäßigen Umgang mit personenbezogenen Daten. [DSGVO, EUR-Lex](https://eur-lex.europa.eu/eli/reg/2016/679/oj)

Die maschinenlesbare Bestandsaufnahme steht in [`i6-context-transfer.json`](../evaluations/i6-context-transfer.json). Sie bildet die tatsächlich implementierte Kette ab:

```text
Open WebUI → n8n → PatchPony MCP → n8n → destination.one
     ↑                                         ↓
     └───────── Antwort, Quellen, Toolnamen ───┘
```

## Festgestellte technische Minimierung

- Die Pipe übermittelt nur die letzte Frage, eine Admin-Projekt-ID, eine Request-ID und ein SHA-256-pseudonymisiertes Open-WebUI-Subjekt.
- Der vollständige Chatverlauf, Klartext-Benutzerattribute und sämtliche Credentials werden nicht an n8n übergeben.
- Der Gateway liest nur manifestfreigegebene, separat read-only gemountete Pfade. `.env`, `.git`, Datenbanken und Hostpfade sind ausgeschlossen.
- Zum Modellprovider gehen die letzte Frage, der feste Read-only-Prompt und nur vom Gateway gelieferte, freigegebene Quellkontexte. Das ist der wesentliche externe Kontexttransfer des Piloten.
- Die n8n-Workflowdefinition speichert weder erfolgreiche noch fehlerhafte Ausführungsdaten. Die echte Instanzkonfiguration, Backups und externe Logs sind damit noch **nicht** abgedeckt.
- Die Nutzeransicht erhält nur Antwort, verifizierte Zitate und erlaubte Toolnamen – keine Rohresultate oder Fehlermeldungen.

Das pseudonymisierte Subjekt bleibt potentiell personenbezogen, weil Open WebUI die Zuordnung herstellen kann. Es ist deshalb nicht als anonyme Kennung zu behandeln.

## Offene Freigaben vor einem echten Benutzerpilot

1. Der fachliche Datenowner bestätigt, dass die ausgewählten Pilotquellen zu `destination.one` übertragen werden dürfen.
2. Datenschutz und Informationssicherheit prüfen Anbieter-/Auftragsverarbeitungsbeziehung, Datenstandort, Unterauftragsverarbeiter, Modelltraining, Protokollierung, Backups und Aufbewahrung für Open WebUI, n8n und destination.one.
3. Für Open WebUI und n8n müssen Produktions-Logging, Backups, Zugriffsrechte und Lösch-/Retentionregeln dokumentiert werden.
4. Keycloak/OIDC ersetzt die vorläufige, deterministische Hash-Pseudonymisierung vor einer breiteren Nutzung.

**Ergebnis:** Die technische Datenminimierung für den eng begrenzten Pilotpfad ist geprüft. Eine formale Freigabe für reale Benutzer- oder Projektinhalte steht weiterhin unter den oben genannten Bedingungen.