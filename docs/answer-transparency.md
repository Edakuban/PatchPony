# Transparenz für Antworten (I6.6)

Die n8n-Antwort enthält neben `answer` zwei optionale, benutzerfreundliche Felder:

- `sources`: ausschließlich strukturierte Zitate aus tatsächlichen `source.search`- oder `source.read`-Toolresultaten;
- `toolCalls`: ausschließlich die Namen erlaubter, tatsächlich verwendeter Read-only-Tools.

Die Open-WebUI-Pipe akzeptiert nur eine enge, formatgeprüfte Teilmenge dieser Daten. Sie begrenzt Quellen auf 20 und Toolnamen auf die feste MCP-Allowlist. Toolparameter, Source-Inhalte, n8n-Zwischenstände, Infrastrukturinformationen und Secrets werden nicht in die Chat-Antwort übernommen.

Wenn keine verifizierte Quelle vorliegt, zeigt die Pipe keine Quellenliste an. Eine vom Modell geschriebene Zitierung allein gilt nicht als nachgewiesene Quelle.