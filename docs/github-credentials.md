# GitHub Bot-Credentials

GitHub-Zugriff wird ausschließlich über `PatchPony:GitHub:RepositoryUri` und `PatchPony:GitHub:Token` konfiguriert. Das Token gehört in die lokale `.env`, einen Secret Store oder eine Deployment-Secret-Variable – niemals in das Repository, Logs, Fehlertexte oder Prozessargumente.

Der Credential-Validator akzeptiert nur einen HTTPS-Repositorypfad auf `github.com` und bindet das Token an genau dieses Repository. Der GitHub-Adapter sendet das Token ausschließlich als `Authorization: Bearer`-Header. Drafts für andere Repository-URIs werden abgewiesen.

Für den GitHub Fine-grained PAT bzw. GitHub-App-Token sind zunächst nur die Rechte für Pull Requests erforderlich; Schreibrechte für Repository-Inhalte werden erst mit dem kontrollierten Push in I10.6 benötigt. Der Token sollte auf das konkrete Zielrepository beschränkt, kurzlebig bzw. rotierbar und einem dedizierten Bot-/Service-Account zugeordnet sein.

Beispiel für eine lokale, nicht eingecheckte `.env`:

```text
PatchPony__GitHub__RepositoryUri=https://github.com/Edakuban/PatchPony
PatchPony__GitHub__Token=<secret>
```
Rotation und die getesteten Redaction-Grenzen sind in [github-credential-rotation.md](github-credential-rotation.md) beschrieben.
