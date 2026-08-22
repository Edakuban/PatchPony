# Projekt-Skills

Projekt-Skills liegen ausschließlich in diesem festen Layout:

```text
.patchpony/skills/<skill-id>/SKILL.md
```

`skill-id` besteht aus Kleinbuchstaben, Ziffern und Bindestrichen. Der
Katalog listet höchstens 100 Verzeichnisse und berücksichtigt nur IDs mit
einer vorhandenen, durch die Manifest-Policy lesbaren `SKILL.md`.

Der Reader akzeptiert nur eine Katalog-ID, keine Pfadangabe. Er nutzt die
kanonische Pfadauflösung, Symlink-Prüfung und Read-Policy, begrenzt den Inhalt
auf 64 KiB und akzeptiert ausschließlich valides UTF-8. Skill-Inhalt wird
nicht geparst, als Befehl behandelt oder ausgeführt.

Damit Skills sichtbar sind, muss das Manifest sie explizit zum Lesen
freigeben, etwa mit `.patchpony/skills/**` in `paths.readable`. Ein Treffer in
`paths.forbidden` überstimmt diese Freigabe.
