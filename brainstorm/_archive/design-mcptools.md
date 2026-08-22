\# tools

workspace.list

workspace.read

workspace.search



workspace.diff

workspace.patch

workspace.write



config.read

config.validate

config.patch



project.describe

project.skills



shell.run









\# policies

read       → immer erlaubt

search     → immer erlaubt

diff       → immer erlaubt



patch      → erlaubt

write      → erlaubt



shell      → eingeschränkt

network    → verboten













\# brainstorm

Viele MCP-Server exponieren sehr generische Tools:



read\_file

write\_file

run\_command



Ich würde PatchPony semantischer machen.



Zum Beispiel:



project.info

project.tree





source.search

source.read





config.list

config.read

config.patch

config.validate





skills.list

skills.read





changes.diff

changes.apply

changes.reset





shell.exec



Der Unterschied scheint klein, ist aber später riesig.



Denn damit kannst du Policies sagen:



allow:

&#x20; source.read: true

&#x20; config.read: true

&#x20; config.patch: true

&#x20; shell.exec: false



anstatt irgendwie herausfinden zu müssen, ob



write\_file("/config/prod.yaml")



erlaubt sein sollte.

