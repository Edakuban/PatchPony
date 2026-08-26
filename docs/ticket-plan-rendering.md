# Rendering von `plan.md`

`TicketPlanMarkdownRenderer` überführt ausschließlich einen bereits validierten `TicketPlanOutput` in die feste Datei `plan.md`. Die Darstellung enthält Ticket-ID, Revision, Projekt, Idempotenzschlüssel, Evidenzreferenzen und die drei Planphasen.

Sie enthält keine Ticketbeschreibung, keine Anhänge, keine Provider-Signatur und keine Modellantwort. Referenzen werden als Code formatiert und Zeilenumbrüche sowie Backticks bereinigt. Der Renderer erzeugt nur einen String; er schreibt weder in einen Worktree noch in ein Git-Repository. Eine spätere kontrollierte Ablage muss die bestehenden Session-, Pfad- und Patchgrenzen verwenden.