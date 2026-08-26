# Change-Request-Kommentare

Nach erfolgreicher Policy-Prüfung erzeugt `ChangeRequestCommentFactory` eine feste, inhaltsarme Statusmeldung für den Zoho-Task:

- Development: kontrollierte Weiterverarbeitung möglich;
- Staging: wartet auf das konfigurierte Review;
- Production: explizite menschliche Freigabe vor Veröffentlichung erforderlich.

Die Vorlage nimmt keine Ticketbeschreibung, Zielreferenzen, Sollwerte, Reviewer-Namen, Provider-Antworten oder Secrets auf. Sie wird erst nach konsistenter Environment- und Reviewer-Anforderung erzeugt und kann danach ausschließlich durch `ZohoTaskCommentService` aus I11.13 zugestellt werden.