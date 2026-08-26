import http from 'k6/http';
import { check, fail, sleep } from 'k6';

const baseUrl = (__ENV.PATCHPONY_LOADTEST_BASE_URL || '').replace(/\/$/, '');
const token = __ENV.PATCHPONY_LOADTEST_N8N_TOKEN || '';
if (__ENV.PATCHPONY_LOADTEST_ACKNOWLEDGE_NON_PRODUCTION !== 'yes') fail('Set PATCHPONY_LOADTEST_ACKNOWLEDGE_NON_PRODUCTION=yes only for an isolated test environment.');
if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/i.test(baseUrl)) fail('PATCHPONY_LOADTEST_BASE_URL must be a local isolated endpoint.');
if (token.length < 20) fail('Set PATCHPONY_LOADTEST_N8N_TOKEN from the isolated test environment.');

const serviceHeaders = { 'X-PatchPony-Service-Token': token };
const mcpHeaders = { ...serviceHeaders, 'Content-Type': 'application/json', Accept: 'application/json, text/event-stream' };

export const options = {
  scenarios: {
    health_parallel: { executor: 'constant-vus', exec: 'health', vus: 10, duration: '2m' },
    rest_parallel: { executor: 'constant-arrival-rate', exec: 'runtimeStatus', rate: 10, timeUnit: '1s', duration: '2m', preAllocatedVUs: 10, maxVUs: 30 },
    mcp_parallel: { executor: 'constant-arrival-rate', exec: 'mcpInitialize', rate: 5, timeUnit: '1s', duration: '2m', preAllocatedVUs: 5, maxVUs: 20 },
  },
  thresholds: {
    'http_req_failed{scenario:health_parallel}': ['rate<0.01'],
    'http_req_duration{scenario:health_parallel}': ['p(95)<250'],
    'http_req_failed{scenario:rest_parallel}': ['rate<0.01'],
    'http_req_duration{scenario:rest_parallel}': ['p(95)<500'],
    'http_req_failed{scenario:mcp_parallel}': ['rate<0.01'],
    'http_req_duration{scenario:mcp_parallel}': ['p(95)<750'],
  },
};

export function health() { const response = http.get(`${baseUrl}/health/ready`); check(response, { 'health is 200': r => r.status === 200 }); sleep(0.1); }
export function runtimeStatus() { const response = http.get(`${baseUrl}/api/v1/runtime/status`, { headers: serviceHeaders }); check(response, { 'runtime status is 200': r => r.status === 200 }); }
export function mcpInitialize() {
  const response = http.post(`${baseUrl}/mcp`, JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'patchpony-load-test', version: '1.0' } } }), { headers: mcpHeaders });
  check(response, { 'MCP initialize is successful': r => r.status === 200 });
}