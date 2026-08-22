# Einheitliche API-Fehler

PatchPony übernimmt fachliche Fehlercodes unverändert aus `DomainError`. Beide Schnittstellen verwenden dieselben Felder:

```json
{
  "code": "validation.invalid",
  "message": "A correlation identifier of at most 128 letters, digits, hyphens or underscores is required.",
  "correlationId": "shared-error-42"
}
```

REST überträgt dieses Objekt direkt und ordnet Codes zusätzlich einem HTTP-Status zu: Validierung `400`, nicht gefunden `404`, verboten `403`, Konflikt `409`, Größen-/Limitüberschreitung `413`, Timeout `504` und nicht verfügbare Abhängigkeiten `503`.

MCP überträgt denselben JSON-Inhalt als Toolresult mit `isError: true`; bei Streamable HTTP kann der Textinhalt im SSE-Wrapper escaped sein. Fachliche Fehler bleiben damit von JSON-RPC-Protokollfehlern getrennt und für LLM-Clients korrigierbar.

Der Vertrag ist durch die äquivalenten Routen `GET /api/v1/runtime/correlations/{correlationId}` und das Tool `runtime.validate_correlation` abgedeckt.
