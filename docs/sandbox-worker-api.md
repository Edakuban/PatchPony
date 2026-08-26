# Interne Sandbox-Worker-API

`ISandboxWorkerApi` ist eine reine Prozessschnittstelle zwischen Dispatcher und Worker. Sie wird weder vom Gateway noch über eine öffentliche HTTP-Route angeboten.

Ein `SandboxJobRequest` enthält nur:

- einen signierten, an den Worker gebundenen `WorkerClaimProof`,
- eine serverseitige Session-ID,
- eine registrierte `commandId` (`[a-z][a-z0-9._-]{0,127}`).

Image, Executable, Shell, Argumente, Mounts und Netzwerkoptionen fehlen absichtlich aus dem Contract. Diese Werte werden ausschließlich serverseitig aus Runner- und Command-Katalogen abgeleitet.

`SandboxWorkerApi` prüft die Claim-Signatur, Ablaufzeit und erwartete Worker-ID. Jede Claim-ID wird nur einmal akzeptiert; Wiederholungen liefern `sandbox.request.replayed`. I9.1 startet noch keinen Prozess oder Container.