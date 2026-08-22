# ADR 0001: MVP-Modellprovider und lokale Authentifizierung

Status: Accepted  
Datum: 2026-08-22

## Entscheidung

- PatchPony verwendet im MVP `https://oi.destination.one/` als OpenAI-kompatiblen Modellprovider.
- Das Testmodell ist das lokal gehostete `gpt-oss:20b`.
- API-Schlüssel und die konkrete API-Basis-URL werden nur über lokale Umgebungsvariablen bereitgestellt.
- Der lokale Entwicklungszugang verwendet zunächst genau ein Passwort aus einer lokalen Umgebungsvariablen.
- Die Modellintegration wird hinter einer Provider-Abstraktion implementiert.

## Konsequenzen

Secrets werden weder committed noch geloggt. Ein Passwort allein ist keine ausreichende Produktionsauthentifizierung.
