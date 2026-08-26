# Kontrollierter Git-Push

Ein Push ist nur für den bestehenden, serverseitig abgeleiteten Session-Branch erlaubt. Vorher liest PatchPony `origin` aus dem Worktree und vergleicht sie mit der registrierten HTTPS-Repository-URI.

Der einzige zulässige Aufruf ist:

```text
git -c core.hooksPath=<disabled> -C <server-worktree> push --porcelain --no-verify origin refs/heads/patchpony/session/<id>:refs/heads/patchpony/session/<id>
```

Force-Push, Tags, geschützte Zielbranches, freie Remote-URLs und frei gewählte Ref-Specs kommen nicht im Contract vor. Eine Origin-Abweichung wird vor dem Push abgelehnt.