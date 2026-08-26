# I14.13 pilot evaluation and V1.1 backlog

## Required input

Evaluate only the redacted aggregate evidence created during I14.12: participant count, question/config-proposal counts, outcomes, p50/p95/p99 latency, queue age, session expiry, policy denials, errors/429s, incidents, manual corrections, source-citation coverage and participant feedback. Do not add ticket text, source snippets, credentials, raw model prompts, user identities or audit payloads.

## Decision matrix

| Area | Measure | Pilot target | V1.1 action if missed |
| --- | --- | --- | --- |
| Reliability | unexpected error rate | below 1% | investigate fault class; do not widen access |
| Latency | p95 REST/MCP | within I14.9 limits | profile/reduce load before more users |
| Safety | unauthorized access/secret exposure/policy bypass | zero | kill switch, incident review, block expansion |
| Workflow quality | source citations accepted by reviewer | at least 90% | improve retrieval/prompt constraints |
| Human effort | manual correction rate | declining trend | prioritize UI/workflow clarity |
| Capacity | queue age and expired sessions | no sustained alert | tune workers/limits or defer expansion |
| Adoption | participant feedback | documented, no severity-1 concern | triage feedback into backlog |

## V1.1 backlog rules

1. Any safety incident, exposure, bypass or failed restore is **P0** and blocks scope expansion.
2. Repeated operational alerts, missing evidence or reliability/latency breaches are **P1**.
3. Repeated reviewer corrections, citation gaps and integration usability issues are **P2**.
4. New capabilities (additional projects, write paths, model access, automation) are **P3** until their threat-model amendment, tests and human approval are complete.
5. Every item carries an owner, evidence reference, acceptance metric and a statement whether it changes a trust boundary.

## Exit decision

The pilot owner and technical reviewer jointly choose one of: **stop and remediate**, **continue unchanged**, or **expand one bounded dimension** (one more user *or* one more project, never both at once). Automatic merge, unreviewed source writes and unrestricted model access remain out of scope regardless of pilot outcome.