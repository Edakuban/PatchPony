# Git-Publikation: Idempotenz und Retry

PatchPony leitet für jede Veröffentlichungsaktion einen stabilen Schlüssel ausschließlich aus Projekt-, Job- und Session-ID sowie der Operation ab. Der Schlüssel enthält keine Client- oder Promptdaten.

## Begrenzte Wiederholung

Es gibt höchstens drei Versuche. Wiederholt werden nur die zuvor normalisierten, transiente Fehlercodes `git_provider.request_failed`, `publication.commit_failed` und `publication.push_failed`. Freigabe-, Zustands-, Branch- und Remotefehler bleiben fail-closed und werden nicht wiederholt. Cancellation beendet die Wiederholung sofort.

## Sichere Operationen

- Commit verwendet einen festen, serverseitig abgeleiteten Text. Vor einem erneuten Commit prüft PatchPony den letzten Commit auf genau diesen Text.
- Push verwendet ausschließlich den fest abgeleiteten Session-Ref-Spec ohne Force und ohne Tags. Ein erneuter Push desselben Refs ist idempotent.
- Vor jedem Create-Versuch eines Merge Requests wird der offene PR für denselben Session-Branch erneut gesucht. Ist er nach einer unsicheren Providerantwort vorhanden, wird er wiederverwendet.

Es gibt keine automatische Zusammenführung und keine Retry-Schleife für beliebige Provider- oder Git-Kommandos.