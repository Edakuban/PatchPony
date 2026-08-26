# Sandbox-Ressourcenlimits

Der Worker verlangt unter `PatchPony:Worker:SandboxLimits` fünf serverseitige Grenzwerte. Fehlende, ungültige oder außerhalb der erlaubten Grenzen liegende Werte verhindern den Start des Workers.

| Einstellung | Docker-Option | erlaubter Bereich |
|---|---|---|
| `CpuCount` | `--cpus` | 0,1 bis 8 CPUs |
| `MemoryBytes` | `--memory` | 64 MiB bis 8 GiB |
| `PidsLimit` | `--pids-limit` | 16 bis 1.024 Prozesse |
| `DiskBytes` | `--storage-opt size=...` | 128 MiB bis 16 GiB |
| `ExecutionTimeout` | Watchdog | 1 Sekunde bis 1 Stunde |

Die Werte gehören ausschließlich zur Worker-Konfiguration; ein Sandbox-Auftrag kann sie weder liefern noch verändern. Der Watchdog beendet nach Ablauf des Limits den Docker-Client inklusive Prozessbaum. Wenn Docker eine gesetzte Ressourcenoption nicht unterstützt oder der Start fehlschlägt, wird kein erfolgreicher Sandbox-Start gemeldet.

Beispiel:

```json
{
  "PatchPony": {
    "Worker": {
      "SandboxLimits": {
        "CpuCount": 1.5,
        "MemoryBytes": 536870912,
        "PidsLimit": 128,
        "DiskBytes": 1073741824,
        "ExecutionTimeout": "00:05:00"
      }
    }
  }
}
```