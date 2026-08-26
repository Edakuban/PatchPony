# Kontrollierte Git-Publikation

`GitSessionPublicationService` arbeitet nur auf einem vorhandenen, symlinkfreien Worktree der aktiven Session. Vor einem Commit prüft er den erwarteten serverseitigen Session-Branch und liest `git status --porcelain=v1 -z`.

Bei Änderungen führt er ausschließlich diese kontrollierten Aktionen aus:

```text
git -C <server-worktree> add --all
git -c core.hooksPath=<server-disabled-hooks> -C <server-worktree> commit --no-verify -m <server-template>
```

Eine Branch-Abweichung, ein leerer Status oder ein Git-Fehler wird abgelehnt. Der Service nimmt weder Pfade, Branches, Commit-Texte noch Git-Argumente vom Client entgegen.