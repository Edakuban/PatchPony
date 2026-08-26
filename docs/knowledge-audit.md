# Knowledge audit trail

`GET /api/v1/runtime/audit/knowledge?limit=100` is a reviewer-only, bounded in-memory trail for Knowledge operations. It complements the generic access-decision audit; it does not replace the append-only operational audit store.

The trail records `tree`, `search`, `read`, `links`, `link-impact`, `ownership-resolved` and `patch-applied` outcomes. Read and link operations carry only a process-scoped HMAC fingerprint of the requested path, the returned immutable revision and bounded result counts. Session changes additionally record added/removed links, incoming and broken-fragment backlink counts, plus HMAC fingerprints of the resolved owner and independent reviewer identities.

It never records Markdown, frontmatter, search queries, raw vault paths, owner names, credentials or model prompts. Fingerprints intentionally change after a Gateway restart; they support correlation inside the retained runtime window, not cross-instance tracking. The trail retains at most 1,000 newest events and is volatile like the existing access audit.

A rejected link-impact event means a proposed change would break a heading fragment; it is evidence of a blocked publication path, not a file mutation. A successful patch records the semantic-operation count and next SHA-256 revision, never the proposed document.