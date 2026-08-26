# Harte Ticket-Automatisierungspolicy

`ZohoTicketAutomationPolicyEvaluator` trifft die Workflowentscheidung ausschließlich aus Triage, strukturiertem Plan und serverkonfigurierter Projektpolicy. Modellantworten, Tickettext und Prompts sind keine Eingaben.

- unvollständig → `needs_information`
- fehlende/ungültige Projektpolicy oder fehlender Plan → `manual_investigation_required`
- Source-Evidenz, fehlende Config-Evidenz, fehlende geforderte Evidenz oder keine explizite Config-Freigabe → `plan_only`
- nur Config-Evidenz plus alle geforderten Evidenzarten und `AllowConfigFix=true` → `change_proposal_ready`

`merge_request_created` kann diese Policy nicht erzeugen. Das folgt erst nach der kontrollierten Session-, Patch-, Test-, Freigabe- und MR-Pipeline aus I11.12.