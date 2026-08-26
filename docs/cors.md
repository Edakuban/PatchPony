# CORS

PatchPony lässt Browser-Zugriffe über andere Origins standardmäßig nicht zu. Ohne `PatchPony:Cors:AllowedOrigins` setzt der Gateway keine CORS-Freigabe-Header.

Falls eine vertrauenswürdige Web-Oberfläche den Gateway aus einer anderen Origin aufrufen muss, wird jede Origin explizit und ohne abschließenden Slash konfiguriert:

```dotenv
PATCHPONY__CORS__ALLOWEDORIGINS__0=https://console.example.com
```

Erlaubt sind ausschließlich exakte `http`- oder `https`-Origins ohne Pfad, Query, Fragment oder eingebettete Zugangsdaten. Wildcards sind nicht vorgesehen. Der Gateway gibt nur `GET` und `POST` sowie die benötigten Request-Header (`Authorization`, `Content-Type`, `X-Correlation-ID` und die lokalen Authentifizierungs-Header) frei. Als Response-Header wird nur `X-Correlation-ID` verfügbar gemacht.

Credential-Sharing ist bewusst nicht aktiviert. Für jeden Browser-Client sollte eine eigene, konkrete Origin konfiguriert werden.