# I14.12 controlled pilot runbook

## Scope

The first pilot is limited to the existing **PatchPony** and **VocaVid** test project registrations, a small named internal user group, read-only questions through Open WebUI/n8n and controlled configuration proposals. Source-code writes, real production repositories, real vault registration, automatic merge and broad model access remain disabled.

## Entry gates

All of the following require recorded evidence before enabling users:

1. Debian baseline, Rootless Docker and nftables audits pass on the intended host.
2. Protected runtime/worker secret files exist at mode 0600 and a credential-rotation rehearsal has been recorded.
3. Backup/restore rehearsal has passed on the intended Docker context; retention dry-run was reviewed.
4. Private Grafana/Prometheus is running and Alertmanager/notification delivery has been tested.
5. I14.9 has a successful isolated k6 aggregate report under the defined latency/error limits.
6. I14.10 conditional review gates are resolved or explicitly risk-accepted by the pilot owner and technical reviewer.
7. n8n, Open WebUI, Zoho and Keycloak routes use only their intended service/user credentials; no secrets are put in n8n workflow data.
8. Two pilot users, an incident lead and a technical reviewer are named; all know the kill-switch and support channel.

## Enablement sequence

1. Record the pilot start, host identifier, build/commit reference, participant pseudonyms and approved project IDs in the private evidence store.
2. Start with one participant and `patchpony` only. Verify one read-only question with a source citation and one denied request (unknown project/path).
3. Add the second participant and `vocavid`; repeat the same checks.
4. Allow controlled config-proposal flow only after both reviewers approve the evidence. Keep publication as human-reviewed PR only.
5. Review dashboard, audit excerpt and n8n results daily. Do not export raw ticket/source/vault content into pilot reports.

## Abort criteria

Immediately execute the I14.11 kill switch and open an incident for: unauthorized project/path access, any credential appearing in logs/workflow data, unexpected sandbox network access, a policy bypass, a suspected data disclosure, failed backup/restore evidence, or sustained high/critical monitoring alert.

## Exit criteria

Run for at least five working days with the declared small group. Record successful/failed questions, policy denials, latency, queue/session state, incidents, manual corrections and participant feedback. I14.13 may begin only from aggregate/redacted evidence; a pilot does not authorize widening project access or automatic merge.