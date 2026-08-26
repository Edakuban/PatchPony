# Interne Worker-Claims

Jeder Queue-Claim ist an eine `ClaimId`, `JobId`, `WorkerId` und eine
ablaufende Lease gebunden. Für die interne Übergabe wird daraus
`WorkerClaimProof` erzeugt. Der Proof enthält zusätzlich seine Ausgabezeit und
eine HMAC-SHA-256-Signatur über alle diese Felder.

Der Worker akzeptiert einen Proof nur, wenn alle folgenden Bedingungen gelten:

- die Signatur ist mit dem dedizierten internen Schlüssel gültig;
- die `WorkerId` entspricht exakt seiner eigenen konfigurierten ID;
- Claim-, Job- und Zeitwerte sind vollständig und plausibel;
- die Lease ist noch nicht abgelaufen.

Manipulierte Werte, ein anderer Worker oder eine ungültige Signatur ergeben
`worker_claim.invalid`; eine abgelaufene Lease ergibt `worker_claim.expired`.
Der Signaturvergleich erfolgt konstantzeitig.

## Konfiguration

Der Worker benötigt beim Start zwingend:

```text
PATCHPONY__WORKER__ID=patchpony-worker-local
PATCHPONY__WORKER__CLAIMSIGNINGKEY=<Base64-kodierte zufaellige 32 Bytes>
```

Der Schlüssel ist ein eigenes internes Secret. Er darf nicht aus
Entwicklungskennwort, OIDC/JWT-, Modellprovider- oder n8n-Token abgeleitet oder
mit ihnen wiederverwendet werden. Er wird nicht geloggt. Der Worker verweigert
den Start bei fehlender, nicht Base64-kodierter oder zu kurzer Konfiguration.

Erst ein künftiger Dispatcher darf Proofs aus einem persistierten Queue-Claim
ausstellen; die spätere tatsächliche Job-Verarbeitung muss vor jeder Wirkung
`Verify` mit der eigenen Worker-ID aufrufen. Der aktuelle Worker besitzt noch
keine Queue-Consumer-Schleife und führt daher keine Jobs aus.