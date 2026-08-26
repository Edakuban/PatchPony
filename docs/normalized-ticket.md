# Neutrales Ticket-Schema

Nach erfolgreicher Zoho-Signatur- und Payloadprüfung mappt der Gateway das Ticket in `NormalizedTicket`. Das Core-Modell kennt nur Provider (`Zoho`), Änderungsart (`Created` oder `Updated`), externe ID, Revision, Titel, Beschreibung und den serverseitigen Eingangszeitpunkt.

Zoho-JSON-Feldnamen, Signaturen und HTTP-Details verlassen den Gateway nicht. Alle Felder sind nochmals begrenzt und kontrollzeichenfest geprüft. Das Schema enthält absichtlich weder eine Zoho-Projektzuordnung noch Modellprompt oder Automatisierungsentscheidung. Anhänge sind auf geprüfte Metadaten und der Kanal auf vier feste Werte begrenzt. Diese Zuständigkeiten folgen in I11.4 bis I11.11.