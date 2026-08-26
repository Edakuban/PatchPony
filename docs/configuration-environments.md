# Konfigurationsumgebungen

I12.2 definiert eine unveränderliche Mindestmatrix für typisierte `Configuration`-Änderungsaufträge:

| Umgebung | Merge Request nach Validierung | Reviewer | explizite menschliche Freigabe vor Veröffentlichung |
|---|---:|---:|---:|
| Development | ja | nein | nein |
| Staging | ja | ja | nein |
| Production | ja | ja | ja |

`EnvironmentConfigurationChangeRequest` kann nur aus einem bereits validierten `TicketChangeRequest` der Art `Configuration` entstehen. Source-Code, Datenbankmigrationen und Secrets können folglich keine Config-Environment-Policy erhalten.

Die Matrix ist ein Core-Vertrag und nicht aus n8n, einem Modell oder dem Tickettext überschreibbar. Die konkrete Zuordnung von Reviewern, zulässigen Schlüsseln und Werten folgt in I12.3 bis I12.5; I12.2 löst weder Validierung noch Veröffentlichung aus.