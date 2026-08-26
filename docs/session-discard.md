# Session verwerfen

I7.8 ergänzt den expliziten, kontrollierten Discard einer Session. Der
Application Service verschiebt eine nicht abgeschlossene Session zunächst nach
`Closing` und persistiert diesen Zwischenstand. Erst wenn die Infrastruktur die
Arbeitskopie erfolgreich entfernt hat, wird `Closed` gespeichert. Scheitert die
Bereinigung, bleibt die Session in `Closing` und kann kontrolliert erneut
verarbeitet werden.

`GitSessionDiscardService` akzeptiert nur den serverseitig konfigurierten
Base-Checkout sowie den aus der Session abgeleiteten Worktree und Branch. Unter
dem kurz gehaltenen Projekt-, Branch- und Pfad-Lock führt er ausschließlich aus:

1. `git worktree remove --force -- <server-worktree>` – falls der Worktree noch
   vorhanden ist;
2. eine feste Branch-Prüfung und anschließend `git branch -D -- <server-branch>`
   – nur wenn der abgeleitete Branch noch existiert;
3. das Entfernen des ausschließlich serverseitig abgeleiteten Sessionordners.

Kein Client liefert Branch, Zielpfad oder freie Git-Argumente. Reparse-Points am
Sessionroot werden vor der lokalen Bereinigung abgelehnt. Git- und Cleanupfehler
haben stabile `session.discard.*`-Codes; die Locks werden auch bei Fehler oder
Cancellation freigegeben.