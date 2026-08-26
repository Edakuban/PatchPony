# Sandbox-Ausgabegrenzen

Der Worker startet Docker mit umgeleitetem stdout und stderr. Beide Streams werden parallel vollständig aus dem Prozess-Pipe geleert, behalten aber jeweils nur die ersten `PatchPony:Worker:SandboxOutput:MaxBytesPerStream` Bytes im Speicher. Das Limit ist serverseitig und muss zwischen 4 KiB und 16 MiB liegen.

Die bereits in `PatchPony:Worker:SandboxLimits:ExecutionTimeout` konfigurierte Laufzeitgrenze gilt auch für die Erfassung. Bei Ablauf beendet der Watchdog den Docker-Prozessbaum; der Collector beendet die Stream-Lektüre und kennzeichnet die Ausgabe als zeitlich begrenzt. Überschüssige Bytes werden verworfen und durch ein Trunkierungsmerkmal kenntlich gemacht, damit ein Container den Worker nicht über ungebremste Konsolenausgabe blockiert.

Die Ausgabe wird nach Prozessabschluss zusammen mit Exit-Code, Ergebnisstatus und sicheren Artefaktmetadaten im serverseitigen Session-Speicher persistiert. Details stehen in [sandbox-results.md](sandbox-results.md).

Beispiel:

```json
{
  "PatchPony": {
    "Worker": {
      "SandboxOutput": {
        "MaxBytesPerStream": 1048576
      }
    }
  }
}
```