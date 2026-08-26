import http from 'k6/http';
import { check, fail } from 'k6';
const baseUrl = (__ENV.PATCHPONY_LOADTEST_BASE_URL || '').replace(/\/$/, '');
const token = __ENV.PATCHPONY_LOADTEST_N8N_TOKEN || '';
if (__ENV.PATCHPONY_LOADTEST_ACKNOWLEDGE_NON_PRODUCTION !== 'yes') fail('Non-production acknowledgement required.');
if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/i.test(baseUrl) || token.length < 20) fail('A local isolated base URL and test token are required.');
export const options = { vus: 20, duration: '20s', thresholds: { 'checks': ['rate>0.99'] } };
export default function () { const response=http.get(`${baseUrl}/api/v1/runtime/status`,{headers:{'X-PatchPony-Service-Token':token}}); check(response, {'rate limit returns 429 rather than an unbounded failure': r=>r.status===200 || r.status===429}); }