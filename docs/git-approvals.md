# Menschliche Freigabe für Veröffentlichungen

`PublicationApprovalGate` wertet pro Projekt eine serverseitige Policy für Push und Merge Request aus. Ist eine Aktion freigabepflichtig, wird die jüngste Freigabe für den zugehörigen Job gelesen; ausschließlich `Approved` erlaubt die Aktion. Fehlende, ausstehende oder abgelehnte Freigaben blockieren fail-closed.

Die vorhandene `approvals`-Persistenz wird über `IApprovalRepository` verwendet. Die Policy enthält keine Clientparameter und wird nicht vom Modell beeinflusst.