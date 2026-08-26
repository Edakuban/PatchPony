# Git-Worktrees für Sessions

I7.3 legt einen Worktree ausschließlich für eine Session im Zustand `Provisioning` an. Der Branchname wird serverseitig aus der Session-ID gebildet:

```text
patchpony/session/<session-id-ohne-trennzeichen>
```

Der Service akzeptiert weder Branch- noch Zielpfade aus Tool- oder API-Eingaben. Er verwendet `git worktree add -b …` mit einzeln übergebenen Argumenten, einem festen 30-Sekunden-Timeout und begrenzter Prozessausgabe. Netzwerkoperationen, Shell-Auswertung und freie Git-Argumente finden nicht statt.

Für den Aufruf überschreibt Git `core.hooksPath` mit einem leeren, sessionlokalen Verzeichnis. Repository-Hooks werden somit nicht ausgeführt. Bei Fehlschlag bleibt der kontrolliert angelegte Sessionordner für I7.10-Crash-Recovery erhalten; Base-Checkout und andere Sessions werden nicht bereinigt oder verändert.

Vor der Worktree-Anlage hält der Service die in [session-locks.md](session-locks.md) definierten Projekt-, Branch- und Pfadleasing-Locks. Sie werden unabhängig vom Ergebnis des kontrollierten Git-Aufrufs wieder freigegeben. Nach erfolgreicher Anlage erzwingt I7.6 außerdem die in [session-limits.md](session-limits.md) dokumentierte Worktree-Größengrenze.