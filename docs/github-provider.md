# GitHub-Provideradapter

`GitHubHostingProvider` implementiert ausschließlich den providerneutralen Merge-Request-Contract für GitHub. Er akzeptiert nur HTTPS-Repository-URIs der Form `https://github.com/<owner>/<repository>` und verwendet die GitHub-REST-Endpunkte zum Suchen offener Pull Requests sowie zum Erstellen eines neuen Pull Requests.

Der Adapter übergibt nur den validierten Draft: Titel, Beschreibung, Source- und Target-Branch. Repository-URI, API-Pfad und HTTP-Methode sind serverseitig abgeleitet. Fehler geben keine Response-Bodies, Header oder Credentials weiter. Authentifizierung und das minimale Bot-Token folgen in I10.3.
Credential-Grenzen und minimale Rechte stehen in [github-credentials.md](github-credentials.md).
