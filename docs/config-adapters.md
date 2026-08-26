# Config-Adapter-Schnittstelle

I8.1 definiert die gemeinsame, rein lesende Grenze für JSON-, YAML- und
XML-Validierung. `IConfigDocumentAdapter` erhält ausschließlich einen
`ConfigValidationRequest` mit:

- einem festen Format (`Json`, `Yaml` oder `Xml`),
- UTF-8-Inhalt als Bytefolge und
- einer optionalen, opaken `SchemaId`.

Der Vertrag enthält ausdrücklich keinen Dateipfad, keine Session-ID, keine
Patch-Operation und keine Schreibfähigkeit. `SchemaId` ist auf einen kurzen,
klein geschriebenen serverseitigen Bezeichner begrenzt; ein rohes Schema oder
Pfadwert kann damit nicht über diese Schnittstelle übergeben werden.

`ConfigAdapterCatalog` wird beim Serverstart aus festen Adapter-Instanzen
zusammengesetzt. Es akzeptiert genau einen Adapter je Format und routet nur an
diesen. Nicht registrierte Formate, ungültige Schema-Referenzen und fehlerhafte
Adapterberichte liefern stabile Fehlercodes. Die konkreten Parser folgen mit
I8.2 (JSON), I8.3 (YAML) und I8.4/I8.5 (XML).