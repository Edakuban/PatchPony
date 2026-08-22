# Projektpfad-Policy

`ProjectPathPolicy` wertet die drei Glob-Listen des validierten Manifests über
kanonische, portable Projektpfade aus. Der Matcher unterstützt `*`, `?` und
`**` ohne Shell oder reguläre Ausdrücke und arbeitet fail-closed.

Die Reihenfolge der Entscheidung ist fest:

1. Unsichere Pfade werden abgewiesen.
2. Ein Treffer in `forbidden` wird immer abgewiesen.
3. Lesen ist nur bei einem Treffer in `readable` erlaubt.
4. Schreiben außerhalb von `writable` wird abgewiesen.
5. Auch innerhalb von `writable` wird Schreiben in I3 mit
   `path.write_disabled` abgewiesen, weil der Base-Checkout read-only bleibt.

`ProjectPathAccessService` kombiniert diese Policy mit
`ProjectPathResolver`: Die Sicherheits- und Symlink-Prüfung erfolgt vor der
Manifestentscheidung.
