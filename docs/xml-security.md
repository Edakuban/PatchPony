# XML-Sicherheit

Der XML-Adapter akzeptiert keine DTD-Deklarationen. Bereits vor dem XML-Reader wird eine DTD mit `xml.dtd_disallowed` als ungültig zurückgewiesen.

Als zusätzliche Schutzschichten konfigurieren sowohl der XML-Reader als auch der serverseitige XSD-Katalog:

- `DtdProcessing.Prohibit`
- `XmlResolver = null`
- `MaxCharactersFromEntities = 0`

Der Regressionstest verwendet eine externe Entity, die auf eine temporäre lokale Datei zeigt. Er stellt sicher, dass das Dokument abgewiesen wird und weder der Dateiinhaltswert noch andere Daten der Entity in der Fehlermeldung auftauchen.