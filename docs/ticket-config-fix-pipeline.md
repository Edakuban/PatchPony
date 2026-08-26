# Kontrollierte Config-Fix-Pipeline

`ConfigFixWorkflowService` definiert die einzige zulässige Reihenfolge für einen von der harten Policy als `change_proposal_ready` eingestuften Config-Fix:

1. serverseitige Session erzeugen,
2. validierten Config-Patch anwenden,
3. ausschließlich registrierte Tests ausführen,
4. Freigabe erzwingen,
5. Merge Request erzeugen.

Jede Stufe ist ein geschlossener Adapterport. Der Orchestrator übergibt nur Manifest-Projekt-ID, Idempotenzschlüssel und die serverseitig abgeleitete Sessionreferenz — keine freien Pfade, Patchinhalte, Testkommandos, Git-Argumente oder Merge-Optionen. Bei abgelehnter Policy startet keine Stufe; beim ersten Fehler wird die Kette beendet.

Die bestehenden Session-, Patch-, Sandbox-, Approval- und Git/MR-Implementierungen bleiben die alleinigen Sicherheitsgrenzen der konkreten Adapter. I11.12 definiert den kontrollierten Handoff; eine reale Proposal-Nutzlast und die Zoho-Kommentarzustellung werden erst in den anschließenden Workflowpaketen verbunden.