# Kontrollierte Merge-Request-Erstellung

`GitMergeRequestService` erzeugt einen Pull Request nur für übereinstimmende serverseitige Projekt-, Job-, Session- und Repositorydaten. Der erwartete Session-Branch und der feste Zielbranch stammen aus den bestehenden Vorlagen bzw. der Repositoryregistrierung.

Vor dem Create-Aufruf fragt der Service den GitHub-Provider nach einem offenen Pull Request für denselben Source- und Target-Branch. Ein vorhandener Pull Request wird zurückgegeben; ein neuer wird nur erzeugt, wenn keiner existiert. Der Service besitzt keine Merge-Operation.

I10.8 erweitert die aktuell minimale Beschreibung um Plan, Diff-Zusammenfassung, Tests und Risiken.