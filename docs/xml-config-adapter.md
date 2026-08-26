# XML-Config-Adapter

I8.4 implementiert `XmlConfigAdapter` für begrenzte, read-only XML-Validierung.
Der Adapter akzeptiert maximal 1 MiB UTF-8-Inhalt und verwendet einen
`XmlReader` mit begrenzter Dokumentgröße. Syntaxfehler werden als strukturierter
`xml.invalid`-Bericht inklusive verfügbarer Zeile und Spalte zurückgegeben.

`XmlSchemaCatalog` enthält ausschließlich beim Serverstart registrierte XSD-
Texte. Jede XSD wird einmal in ein `XmlSchemaSet` geparst und kompiliert. Eine
`SchemaId` kann nur diesen Katalog adressieren; externe Schema-URIs, lokale
Schema-Pfade und Roh-XSDs sind nicht Teil des Contracts. Schemaabweichungen
liefern `xml.schema.invalid`.

Die Reader- und Schemaeinstellungen sind bereits so aufgebaut, dass externe
Resolver fehlen und DTD-Verarbeitung verboten ist. I8.5 ergänzt dafür die
expliziten XXE-/DTD-Regressionstests und die abschließende Security-Dokumentation.