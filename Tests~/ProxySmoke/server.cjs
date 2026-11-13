// Test-only proxy. Never use real credentials; it records requests in memory.
const http = require('node:http');
const https = require('node:https');
const fs = require('node:fs');
const assert = require('node:assert/strict');

const runs = new Map();
const handler = (req, res) => {
  const url = new URL(req.url, 'http://localhost');
  const run = url.searchParams.get('run');
  res.setHeader('Content-Type', 'application/json');
  if (!run) { res.writeHead(400).end('{}'); return; }
  if (req.method === 'GET' && url.pathname === '/report') {
    const requests = runs.get(run) || [];
    const errors = [];
    const route = '/lcf/statsig/client/v1/';
    const paths = ['initialize', 'events', 'initialize', 'events', 'initialize', 'events'];
    const tokens = ['Bearer smoke-A', 'Bearer smoke-A', 'Bearer smoke-B', 'Bearer smoke-B', undefined, undefined];
    try {
      assert.equal(requests.length, 6);
      requests.forEach((request, i) => {
        assert.equal(request.method, 'POST');
        assert.equal(request.url, `${route}${paths[i]}?run=${run}&encoded=a%2Fb`);
        assert.equal(request.headers.authorization, tokens[i]);
        assert.equal(request.headers['lcf-client-app-id'], 'unity-smoke');
        assert.equal(request.headers['statsig-api-key'], 'client-proxy-smoke');
        assert.match(request.headers['content-type'], /^application\/json/);
        assert.ok(Number(request.headers['statsig-client-time']) > 0);
      });
      assert.equal(requests[0].body.user.userID, 'smoke-a');
      assert.equal(requests[2].body.user.userID, 'smoke-b');
      assert.equal(requests[4].body.user.userID, 'smoke-c');
      assert.equal(requests[1].body.events[0].eventName, 'smoke-a');
      assert.equal(requests[3].body.events[0].eventName, 'queued-before-rotation');
      assert.equal(requests[5].body.events[0].eventName, 'shutdown-event');
    } catch (error) { errors.push(error.message); }
    res.end(JSON.stringify({ ok: !errors.length, requestCount: requests.length, errors: errors.join('\n') }));
    runs.delete(run);
    return;
  }
  if (req.method !== 'POST') { res.writeHead(405).end('{}'); return; }
  let raw = '';
  req.on('data', (chunk) => { raw += chunk; });
  req.on('end', () => {
    try {
      const requests = runs.get(run) || [];
      requests.push({ url: req.url, method: req.method, headers: req.headers, body: JSON.parse(raw) });
      runs.set(run, requests);
      res.end(JSON.stringify({ has_updates: true, time: 1, feature_gates: {}, dynamic_configs: {}, layer_configs: {}, sdkParams: {} }));
    } catch { res.writeHead(400).end('{}'); }
  });
};

const server = process.env.TLS_CERT && process.env.TLS_KEY
  ? https.createServer({ cert: fs.readFileSync(process.env.TLS_CERT), key: fs.readFileSync(process.env.TLS_KEY) }, handler)
  : http.createServer(handler);
server.listen(Number(process.env.PORT || 8787), process.env.HOST || '127.0.0.1', () => {
  console.log(`Smoke proxy listening on ${JSON.stringify(server.address())}`);
});
