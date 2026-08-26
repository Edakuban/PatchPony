# JSON-Config-Adapter

I8.2 implementiert `JsonConfigAdapter` als read-only Adapter für
`ConfigDocumentFormat.Json`. Er parst ausschließlich die übergebene UTF-8-
Bytefolge mit strikt deaktivierten Kommentaren und Trailing-Commas sowie einer
maximalen Verschachtelungstiefe von 64.

Der Adapter begrenzt seinen Parserinput auf 1 MiB. Syntax- oder Größenfehler
werden als strukturierter ungültiger Bericht (`json.invalid` beziehungsweise
`json.too_large`) zurückgegeben; sie verändern keine Datei und lösen keine
Nebenwirkung aus.

Ist eine `SchemaId` gesetzt, löst `JsonSchemaCatalog` sie nur aus einem beim
Serverstart fest registrierten Katalog auf. Der Adapter akzeptiert weder
Schema-URLs noch Schema-Dateipfade oder Rohschemawerte. Die Validierung nutzt
JsonSchema.Net; eine Schemaabweichung liefert `json.schema.invalid`, eine nicht
registrierte ID `config.schema.unsupported`.