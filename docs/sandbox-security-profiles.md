# Zusätzliche Sandbox-Sicherheitsprofile

Neben non-root, read-only Root-FS und `cap-drop=ALL` fügt jeder Sandbox-Start zwingend hinzu:

```text
--security-opt no-new-privileges:true
--security-opt seccomp=<absoluter-serverpfad>
--security-opt apparmor=<serverprofil>
```

Die Werte stammen ausschließlich aus `PatchPony:Worker:SandboxSecurity`. `SeccompProfilePath` muss ein vorhandener absoluter Hostpfad sein. `AppArmorProfile` ist ein eng validierter Profilname; das Profil muss vom Betreiber auf dem Docker-Linux-Host installiert sein. Fehlt eine dieser Voraussetzungen, wird der Sandbox-Launcher fail-closed nicht erzeugt.

Für lokale Windows-Entwicklung wird die tatsächliche Docker-/Linux-Profilinstallation erst mit der Rootless- und Runner-Umgebungsprüfung in I9.12 verifiziert. Diese Konfiguration gehört in die Environment-/Secret-Konfiguration, nicht ins Repository.