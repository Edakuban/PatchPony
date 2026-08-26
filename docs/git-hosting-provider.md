# Git-Hosting-Providervertrag

PatchPony trennt die providerneutrale Publication-Logik von der API eines konkreten Hosts. `IGitHostingProvider` hat nur zwei Aufgaben: einen vorhandenen offenen Merge Request für einen serverseitigen Draft finden und einen neuen erstellen.

Der Draft ist vollständig serverseitig gebunden: Projekt-, Job- und Session-ID, HTTPS-Repository-URI, Source- und Target-Branch, Titel und Beschreibung. Branches werden validiert, dürfen nicht identisch sein, und Texte sind begrenzt. Provider-Credentials, HTTP-Header, API-spezifische Felder und Remote-URLs aus Modell- oder Client-Eingaben liegen nicht im Contract.

Der aktuell konfigurierte Remote dieses Projekts ist GitHub (`github.com/Edakuban/PatchPony`); I10.2 implementiert deshalb ausschließlich den GitHub-Adapter. GitLab bleibt nur als explizite Contract-Variante erhalten und wird nicht gleichzeitig implementiert.
Der konkrete Adapter und seine HTTP-Contracts sind in [github-provider.md](github-provider.md) beschrieben.
