# Pilotquellen für den Read-only-Agenten

I6.5 stellt drei lokal gemountete, projektgebundene Quellen bereit: `patchpony`, `vocavid` und `one-data`. Das MCP-Tool `projects.list` zeigt ausschließlich die Projekt-ID und den Anzeigenamen der für das aufrufende Dienstkonto freigegebenen Quellen. Die MCP-Tools `source.search` und `source.read` akzeptieren immer eine Projekt-ID und prüfen zusätzlich die Projekt-Claims des aufrufenden Dienstkontos.

Jede Quelle besitzt ein eigenes Manifest unter `integrations/pilot-projects/`. Dieses ist die verbindliche Allowlist. Nicht freigegebene Pfade, insbesondere `.env`, `.git`, Datenbanken und lokale Konfiguration, sind auch über die MCP-Tools nicht lesbar.

`docker-compose.yml` montiert nur die erlaubten Ordner und Dateien in den Gateway-Container. Somit gelangt die lokale `.env` technisch nicht in den Container. VocaVid liegt bewusst nur als ignorierter, lokaler Checkout in `pilot-sources/VocaVid`.

Die MCP-Rückgaben liefern Zitate strukturiert als `projectId`, `path`, `startLine` und `endLine`. Der n8n-Agent soll Quellen nur im Format `[projekt:pfad:Lzeile]` nennen, wenn diese Werte tatsächlich aus einem Toolresultat stammen.

## Lokale Vorbereitung

```text
PATCHPONY__AUTH__N8N__PROJECTS=patchpony,vocavid
```

Danach den Compose-Stack neu bauen/starten. Fehlt ein gemounteter Checkout oder sein Manifest, startet der Gateway fail-closed nicht mit einer unvollständigen Quellenkonfiguration.