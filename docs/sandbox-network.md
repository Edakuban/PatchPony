# Netzwerkisolierung der Sandbox

Jeder Sandbox-Container startet zwingend mit:

```text
--network none
```

Damit stehen weder Internet, DNS noch andere Docker-Netzwerke zur Verfügung. Die Einstellung ist fest im Worker-Launcher verankert und fehlt absichtlich aus allen Request-, Command- und Runner-Contracts. Sie kann folglich nicht durch Modell-, Ticket- oder Client-Eingaben überschrieben werden.