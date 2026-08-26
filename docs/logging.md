# Logging und Redaction

PatchPony protokolliert für Betrieb und Sicherheitsanalyse nur strukturierte, minimale Auditdaten. Request-Bodies, Tool-Parameter und Header werden nicht in die Audit-Ereignisse übernommen.

Zusätzlich maskiert der zentrale `GatewayLogRedactor` Werte, die wie Zugangsdaten oder Ticketinhalte aussehen. Dazu zählen insbesondere `Authorization`-/Bearer-Werte, Tokens, API-Keys, Passwörter, Secrets sowie Ticket- und Issue-Beschreibungen. Der Platzhalter lautet `[REDACTED]`.

Neue Logstellen dürfen keine Rohwerte aus Request-Headern, Bodies, Tool-Parametern oder Providerantworten ausgeben. Falls ein solcher Wert für die Diagnose unvermeidbar ist, muss er vor dem Loggen über `IGatewayLogRedactor` laufen. Die in `/api/v1/runtime/audit/access` abrufbaren Audit-Ereignisse verwenden dieselben redigierten Werte.