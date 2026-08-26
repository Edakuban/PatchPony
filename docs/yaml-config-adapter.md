# YAML-Config-Adapter

I8.3 implementiert `YamlConfigAdapter` für read-only YAML-Validierung. Der
Adapter dekodiert ausschließlich striktes UTF-8, begrenzt den Inhalt auf 1 MiB
und prüft den YAML-Eventstrom vor jeder Deserialisierung.

Anchors und Aliases sind vollständig verboten (Limit 0), damit Alias-Bombs und
zyklische Datenstrukturen ausgeschlossen sind. Explizite Tags sowie mehrere
YAML-Dokumente sind ebenfalls nicht erlaubt. Zusätzlich begrenzt der Adapter
den Event-/Node-Strom auf 100.000 Knoten.

Erst nach diesen Prüfungen wird YAML ohne Typbindung in JSON-kompatible Daten
überführt. Optional validiert der Adapter sie mit dem aus I8.2 bekannten,
serverseitig registrierten JSON-Schema-Katalog. Schema-URLs, Pfade und
Rohschemas gehören nicht zum Adaptervertrag.