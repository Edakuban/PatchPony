# Source-Reader

`SourceReader` liest genau eine durch die Manifest-Policy freigegebene,
kanonische Projektdatei. Das Ergebnis enthält ausschließlich den relativen
Projektpfad und nummerierte Textzeilen.

Der Reader akzeptiert nur valides UTF-8, lehnt Dateien mit Nullbytes als binär
ab und verweigert Dateien über 128 KiB. Höchstens 500 Zeilen werden geliefert;
`IsTruncated` signalisiert weitere Zeilen. Diese Grenzen verhindern, dass
Source-Reads unkontrolliert Kontext oder Binärdaten ausgeben.

Der Reader führt keinen Dateiinhalt aus und akzeptiert weder absolute Pfade
noch Pfadtraversierung oder Pfade außerhalb der Read-Policy.
## Cancellation und Timeout

`ReadAsync` nimmt ein `CancellationToken` entgegen und verwendet zusätzlich ein
internes Zeitlimit von fünf Sekunden. Eine vom Aufrufer ausgelöste Abbruchanforderung
wird als `OperationCanceledException` weitergegeben; ein internes Zeitlimit ergibt
den fachlichen Fehler `source.timeout`.
