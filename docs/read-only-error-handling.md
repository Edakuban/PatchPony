# Fehlermeldungen im Read-only-Pilot (I6.8)

Die Antwortkette klassifiziert nur wenige, erwartbare Fehler. Sie gibt niemals Rohfehler, HTTP-Details, Hostnamen, Stacktraces, Token, Toolparameter oder Source-Inhalte aus.

| Situation | Nachricht im Chat |
|---|---|
| Zeitlimit erreicht | „PatchPony hat nicht rechtzeitig geantwortet. Bitte versuche es erneut.“ |
| Agentenlimit erreicht | „PatchPony hat das Recherchelimit erreicht. Bitte grenze die Frage weiter ein und versuche es erneut.“ |
| Anfrage abgebrochen | „Die PatchPony-Anfrage wurde abgebrochen. Bitte stelle die Frage erneut, wenn du fortfahren möchtest.“ |
| Rate Limit / Überlastung | „PatchPony ist gerade ausgelastet. Bitte warte kurz und versuche es erneut.“ |
| Anderer Fehler | generische Verarbeitungs- oder Erreichbarkeitsmeldung |

Open WebUI wartet höchstens 60 Sekunden auf n8n. Der n8n-Agent übergibt Fehler kontrolliert an seinen Format-Schritt; der Webhook bleibt damit für den Nutzer antwortbar. Eine abgebrochene Python-Task wird absichtlich nicht abgefangen.