# Zoho-Task-Kommentare

`ZohoTaskCommentService` stellt Ergebnis- und Link-Kommentare ausschließlich an eine explizit konfigurierte Zoho-Aufgabe zu. Die Nutzlast wird vor dem Netzwerkzugriff als `TicketComment` validiert: maximal 2.000 Zeichen Ergebnistext, höchstens acht eindeutige HTTPS-Links, keine Zugangsdaten und keine URL-Fragmente.

```text
PATCHPONY__ZOHO__APIBASEURI=https://<regionaler-projectsapi-host>
PATCHPONY__ZOHO__PORTALID=hubermedia
PATCHPONY__ZOHO__ACCESSTOKEN=<nur lokal>
```

Der OAuth-Token benötigt `ZohoProjects.tasks.CREATE`. PatchPony sendet ein formularcodiertes `content`-Feld an die von Zoho für Task-Kommentare dokumentierte Route:

```text
POST /restapi/portal/{portal}/projects/{project}/tasks/{task}/comments/
Authorization: Bearer <token>
```

Die veröffentlichte V3-Dokumentation enthält derzeit keinen entsprechenden Task-Kommentar-Endpunkt. Deshalb ist die dokumentierte Kommentarroute in diesem schmalen Adapter gekapselt; der Rest des Task-Vertrags und alle n8n-Payloads bleiben V3-orientiert. Nur ein erfolgreicher HTTP-Status gilt als Zustellung. Konfigurations-, Transport- und Providerfehler liefern stabile Fehlercodes ohne Response-Body, Kommentarinhalt oder Token zu protokollieren.

Der Webhook selbst erzeugt absichtlich noch keinen Kommentar. Der Adapter nimmt nur bereits validierte Ergebnisdaten aus dem kontrollierten Workflow entgegen; das verhindert freie externe Kommentartexte.