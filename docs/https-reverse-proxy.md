# TLS über Caddy Reverse Proxy

Im Compose-Stack ist der Gateway-Port nicht mehr auf dem Host veröffentlicht.
Nur Caddy veröffentlicht einen TLS-Port und leitet intern auf `gateway:8080`
weiter. Caddy setzt dabei die üblichen `X-Forwarded-*`-Header selbst und
akzeptiert keine vom Client gefälschten Werte dafür.

## Lokal

Der Standard-Stack verwendet `deploy/caddy/Caddyfile.local` mit `tls internal`.
Damit ist PatchPony über `https://localhost:8443` erreichbar (oder den in
`GATEWAY_HTTPS_PORT` gesetzten Port). Caddy erzeugt eine lokale CA; deren
Root-Zertifikat muss bei Browsern oder lokalen API-Clients einmalig als
vertrauenswürdig installiert werden. Bis dahin ist ein Zertifikatswarnhinweis
bei lokalen Tests erwartbar und kein Grund, TLS-Prüfung im Client zu umgehen.

```powershell
docker compose up --build
docker compose cp caddy:/data/caddy/pki/authorities/local/root.crt .\\caddy-local-root.crt
curl.exe --cacert .\\caddy-local-root.crt https://localhost:8443/health/ready
```

Der lokale CA-Speicher liegt im Docker-Volume `caddy-data`. Das Löschen dieses
Volumes rotiert die lokale CA und erfordert erneutes Vertrauen beim Client.

## Produktion

Für eine öffentliche Domain mit DNS- und Firewall-Freigabe für Port 80 und 443
wird die Produktions-Überlagerung verwendet:

```powershell
$env:PATCHPONY_PUBLIC_HOST = "patchpony.example.com"
docker compose -f docker-compose.yml -f deploy/caddy/docker-compose.production.yml up --build -d
```

`Caddyfile.production` verwendet automatische öffentliche Zertifikate und
HTTPS-Redirects. `PATCHPONY_PUBLIC_HOST` ist absichtlich ohne Defaultwert;
eine fehlende Domain lässt die Proxy-Konfiguration ungültig werden. Die
Portfreigabe, DNS-Zuordnung und das Zertifikatsmonitoring liegen beim
Deployment-Verantwortlichen.

Die Verbindung Caddy → Gateway bleibt innerhalb des privaten Compose-Netzes
HTTP. Das ist vertretbar, weil der Gateway keine Host-Port-Freigabe besitzt;
eine spätere Aufteilung auf verschiedene Hosts benötigt zusätzlich mTLS oder
ein gleichwertig abgesichertes internes Netzwerk.