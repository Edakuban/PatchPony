# Projektbasierte Reviewer-Zuordnung

Reviewer werden über eine geschlossene `ReviewerAssignmentPolicy` pro Projekt festgelegt. Jede Policy enthält mindestens einen validierten GitHub-Username; unbekannte Projekte oder ungültige Namen werden abgelehnt.

Nach dem Find/Create des Pull Requests ruft der GitHub-Adapter ausschließlich mit dieser Policy den Endpoint für `requested_reviewers` auf. Ein Modell, MCP-Client oder Ticket kann keine Reviewer übergeben oder überschreiben.