# Kanonische Projektpfade

`ProjectPathResolver` wird mit einem absoluten, serverseitig bestimmten
Checkout-Root erzeugt. Er akzeptiert ausschließlich relative Pfade und löst
sie mit diesem Root zu einem kanonischen Ziel auf.

Das Ergebnis enthält einen portablen relativen Pfad mit `/`-Separatoren und
den internen vollständigen Pfad für nachgelagerte Infrastrukturadapter.
Fehler enthalten niemals den Checkout- oder einen anderen Host-Pfad.

Pfadsegmente wie `.` und redundante `/`-Separatoren werden normalisiert. Rohes
`..`, absolute Eingaben und Backslashes werden vor der Auflösung abgewiesen.
Jeder vorhandene Pfadbestandteil wird auf Reparse Points/Symlinks geprüft; der
finale Link-Zielpfad muss innerhalb des Checkout-Roots liegen. Die Readable-/Writable-/Forbidden-Policy wird nach der sicheren Auflösung ausgewertet.