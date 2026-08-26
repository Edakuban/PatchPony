# Session-Locks

I7.5 schützt die kurze, mutierende Provisionierungsphase einer Session mit drei
dateibasierten Leases unter `<SESSION_STORAGE_ROOT>/locks`:

- `projects/<project-id>.lock` verhindert gleichzeitige konfliktträchtige Arbeiten
  für dasselbe Projekt.
- `branches/<sha-256>.lock` schützt den ausschließlich serverseitig abgeleiteten
  Branch `patchpony/session/<session-id>`.
- `paths/<sha-256>.lock` schützt den ebenfalls serverseitig abgeleiteten
  Worktree-Pfad.

Die Inhalte sind JSON-Metadaten mit Scope, abgeleiteter Ressource, Session als
Eigentümer, Lease-ID, Ausgabezeit und Ablaufzeit. Die Namen der gehashten
Branch- und Pfadlockdateien enthalten keine beliebigen Eingabewerte. Locks werden
per atomarem `CreateNew` angelegt. Ist ein valider Lock abgelaufen, wird er per
atomarem Rename aus dem Weg genommen; ein anderer Eigentümer oder eine andere
Lease-ID kann einen bestehenden Lock nie freigeben. Unlesbare oder unvollständige
Lockdateien werden konservativ weiterhin als belegt behandelt.

Die maximale Lease-Dauer beträgt 15 Minuten. I7.6 bindet diese Sperren an die
konkrete Provisionierungs- und Ablaufdurchsetzung.