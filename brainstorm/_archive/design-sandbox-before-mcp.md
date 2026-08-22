Ein häufiger Architekturfehler wäre:



PatchPony = MCP server



Ich würde eher sagen:



PatchPony = Runtime

MCP = ein Interface zur Runtime



Intern beispielsweise:



patchpony-core





patchpony-mcp

&#x20;   ↓

patchpony-core

&#x20;   ↓

patchpony-sandbox



Warum?



Heute willst du MCP.



Später willst du vielleicht:



patchpony inspect

patchpony run pytest

patchpony diff



oder eine REST/API:



POST /cells/{id}/exec



oder einen Daemon.



Dann musst du deine Business Logic nicht aus dem MCP-Server herausoperieren.















Und ich würde Änderungen von Anfang an session-basiert machen



Das halte ich sogar für eine der wichtigsten Architekturentscheidungen.



Nicht:



MCP → echtes Projekt verändern



sondern:



&#x20;               project

&#x20;                  │

&#x20;               read-only

&#x20;                  │

&#x20;         ┌────────┴────────┐

&#x20;         ▼                 ▼

&#x20;     session A         session B

&#x20;        COW               COW

&#x20;         │                 │

&#x20;       diff              diff



Damit kann ein Agent theoretisch komplett Amok laufen und:



rm -rf .



machen.



Resultat:



Seine Session ist kaputt.



Nicht:



Dein Repository ist kaputt.



boxsh bringt genau so einen COW-Mechanismus bereits mit.

