# Negative Policy-Testmatrix

Die Matrix bildet die Ablehnungen ab, die Security Gate A für extern erreichbare Read-only-Schnittstellen verlangt. Sie läuft mit `dotnet test tests/PatchPony.PolicyTests/PatchPony.PolicyTests.csproj`.

| Szenario | Erwartung | Automatischer Test |
|---|---|---|
| Keine, falsche oder nicht akzeptierte Authentifizierung | `401`, keine Projektinformationen | `GatewayHealthEndpointTests.DefaultDeny_...`, `RuntimeIdentity_Requires...`, `RuntimeIdentity_Authenticates...` |
| Benutzer ohne Projektclaim | `authorization.forbidden` | `NegativePolicyMatrixTests` |
| n8n außerhalb der Projekt-Allowlist | `authorization.forbidden` | `NegativePolicyMatrixTests` |
| Fehlender Tool-Scope | `authorization.forbidden` | `NegativePolicyMatrixTests` |
| Manipulierte Projekt- oder Tool-ID | `authorization.forbidden` | `NegativePolicyMatrixTests` |
| Nicht erlaubte, fehlende oder zu große Tool-Parameter | `authorization.forbidden`; Parameterwerte nie im Audit | `NegativePolicyMatrixTests` |
| Flooding | `429` mit `Retry-After` | `GatewayHealthEndpointTests.RateLimit_...` |
| Nicht konfigurierte oder fremde Browser-Origin | keine CORS-Freigabe | `GatewayHealthEndpointTests.Cors_...` |
| Klartext-Credentials oder Ticketdaten in Audit/Logs | redigiert | `GatewayLogRedactorTests` |

Damit ist die negative Testmenge nachvollziehbar einer konkreten, automatisierten Regression zugeordnet.