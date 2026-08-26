# Agentenlimits (I6.7)

Der Read-only-Agent besitzt für den Pilotbetrieb eine harte Obergrenze von **sechs Iterationen** (`maxIterations: 6`). Da eine Agenteniteration höchstens einen Toolaufruf ausführen kann, sind maximal sechs MCP-Toolaufrufe pro Benutzeranfrage möglich. Das begrenzt Kosten, Laufzeit und die Menge lesbarer Quellen unabhängig von Modellentscheidungen.

Weitere, unabhängige Grenzen:

- Open-WebUI-Pipe: maximal 60 Sekunden Wartezeit auf n8n;
- MCP-Client in n8n: maximal 60 Sekunden pro Aufruf;
- Gateway: 30 MCP-Requests je authentifiziertem Service-Account und 60 Sekunden;
- UI-Transparenz: maximal sechs Toolnamen und 20 verifizierte Quellen.

Wenn das Iterationslimit erreicht wird, behandelt der nachfolgende I6.8-Fehlerpfad dies als nutzerfreundliche, wiederholbare Einschränkung. Der Workflow bleibt ohne Schreibwerkzeuge.