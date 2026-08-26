# Load and concurrency testing

I14.9 supplies two k6 tests for an isolated PatchPony environment. They are deliberately prevented from running against a remote or production hostname: `PATCHPONY_LOADTEST_BASE_URL` must be `localhost` or `127.0.0.1`, an explicit acknowledgement is required, and the n8n service token is supplied only through the caller environment.

## Prerequisite

Start a disposable local/test stack with a fresh database and temporarily increased API/MCP rate-limit settings. Do not change a production environment for a load test. The main scenario sends 10 REST requests/s and 5 MCP initialize requests/s for two minutes, so the test stack must set both permit limits above 1,200 per minute. Keep the production defaults unchanged.

```bash
export PATCHPONY_LOADTEST_ACKNOWLEDGE_NON_PRODUCTION=yes
export PATCHPONY_LOADTEST_BASE_URL=https://127.0.0.1:8443
export PATCHPONY_LOADTEST_N8N_TOKEN=<isolated-test-token>
k6 run deploy/load/k6-gateway.js
```

The main test covers parallel readiness checks, authenticated REST status reads, and stateless MCP initialization. It fails at more than 1% errors or above the documented p95 latency limits (250 ms health, 500 ms REST, 750 ms MCP). Save only the aggregate k6 JSON/HTML output in the pilot evidence store; do not include headers, tokens, request bodies or production identifiers.

Run the separate rate-limit scenario without increasing limits to prove overload handling returns `429` rather than unbounded work or 5xx responses:

```bash
k6 run deploy/load/k6-rate-limit.js
```

Record host sizing, test-stack configuration, concurrency, p50/p95/p99, error/429 ratio and the dashboard state. A production pilot needs an actual evidence record from this procedure before changing the workload or capacity limits.