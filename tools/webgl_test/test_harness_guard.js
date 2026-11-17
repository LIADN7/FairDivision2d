// Tests for harness_guard.js in a stubbed browser context (Node, no dependencies, no network).
//   node tools/webgl_test/test_harness_guard.js
'use strict';
const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const SRC = fs.readFileSync(path.join(__dirname, 'harness_guard.js'), 'utf8');
const POLICY = {
  photonSuffixes: ['.photonengine.io', '.exitgames.com', '.photonengine.com'],
  authHosts: ['player-auth.services.api.unity.com'],
  cloudSaveHosts: [],
  appVersion: 'uxtest_2.42', punVersion: '2.42', environment: 'development',
};

function page(mode, policy) {
  const posted = [];
  const sockets = [];
  class FakeWS {
    constructor(url) { this.url = url; this.sent = []; this.closed = false; sockets.push(this); }
    send(d) { this.sent.push(d); }
    close() { this.closed = true; }
  }
  class FakeXHR {
    open(m, u) { this.target = u; }
    setRequestHeader() {}
    send() { this.sentTo = this.target; }
  }
  const quiet = () => {};
  const ctx = {
    location: { href: 'http://127.0.0.1:8765/index.html', origin: 'http://127.0.0.1:8765' },
    URL, setTimeout, Promise, JSON, Date, TypeError, Uint8Array, ArrayBuffer,
    console: { log: quiet, info: quiet, warn: quiet, error: quiet },
    navigator: {}, XMLHttpRequest: FakeXHR, WebSocket: FakeWS,
    fetch: (u, init) => { posted.push({ url: String(u), body: init && init.body }); return Promise.resolve({}); },
    document: { body: { appendChild: quiet }, addEventListener: quiet, createElement: () => ({ style: {} }) },
  };
  ctx.window = ctx;
  vm.createContext(ctx);
  vm.runInContext(SRC.replace('__UXTEST_MODE__', mode).replace('__UXTEST_POLICY__', JSON.stringify(policy || POLICY)), ctx);
  return { ctx, posted, sockets, state: ctx.__uxtest, logs: () => posted.filter(p => p.url === '/__uxtest/log').map(p => JSON.parse(p.body)) };
}

const tests = {
  'offline: Photon, Unity Auth and other hosts are all refused, the auth header is recorded'() {
    const p = page('offline');
    const ws = new p.ctx.WebSocket('wss://ns.photonengine.io:443/');
    assert.strictEqual(ws.readyState, 3);
    assert.strictEqual(p.sockets.length, 0, 'no real socket in offline mode');
    const x = new p.ctx.XMLHttpRequest();
    x.open('POST', 'https://player-auth.services.api.unity.com/v1/authentication/anonymous');
    x.setRequestHeader('UnityEnvironment', 'development');
    x.send('');
    assert.strictEqual(x.sentTo, '/__uxtest/blocked');
    const auth = p.state.attempts.find(a => a.url.indexOf('player-auth') >= 0);
    assert.strictEqual(auth.UnityEnvironment, 'development');
    assert.ok(auth.decision.startsWith('BLOCKED'));
    let rejected = false;
    p.ctx.fetch('https://operate-sdk-telemetry.unity3d.com/x').catch(() => { rejected = true; });
    return Promise.resolve().then(() => assert.ok(rejected));
  },
  'offline: same-origin files pass untouched'() {
    const p = page('offline');
    const x = new p.ctx.XMLHttpRequest();
    x.open('GET', 'Build/b.data');
    x.send();
    assert.strictEqual(x.sentTo, 'Build/b.data');
  },
  'online: Photon before the TEST MODE AppVersion line is a STOP'() {
    const p = page('online');
    const ws = new p.ctx.WebSocket('wss://ns.photonengine.io:443/');
    assert.strictEqual(ws.readyState, 3);
    assert.ok(p.state.stopped && p.state.stopped.indexOf('before TEST MODE AppVersion') >= 0);
  },
  'online: wrong AppVersion line is a STOP and blocks Photon'() {
    const p = page('online');
    p.ctx.console.log('TEST MODE: Photon AppVersion = _2.42');
    assert.ok(p.state.stopped);
    new p.ctx.WebSocket('wss://ns.photonengine.io:443/');
    assert.strictEqual(p.sockets.length, 0);
  },
  'online: Photon allowed after uxtest_2.42; a production AppVersion frame is a STOP and closes it'() {
    const p = page('online');
    p.ctx.console.log('TEST MODE: Photon AppVersion = uxtest_2.42 (lobby load, connected=False)');
    const ws = new p.ctx.WebSocket('wss://ns.photonengine.io:443/');
    assert.strictEqual(p.sockets.length, 1);
    ws.send(new Uint8Array([7, 11]).buffer);
    ws.send(Uint8Array.from(Buffer.from('\x07\x0buxtest_2.42', 'latin1')));
    assert.strictEqual(p.state.wireAppVersion, 'uxtest_2.42');
    assert.strictEqual(p.sockets[0].sent.length, 2);
    ws.send(Uint8Array.from(Buffer.from('\x07\x05_2.42', 'latin1')));
    assert.ok(p.state.stopped && p.state.stopped.indexOf('"_2.42"') >= 0);
    assert.strictEqual(p.sockets[0].sent.length, 2, 'the production frame was not sent');
    assert.ok(p.sockets[0].closed);
  },
  'online: Unity Auth only with UnityEnvironment development'() {
    const ok = page('online');
    const a = new ok.ctx.XMLHttpRequest();
    a.open('POST', 'https://player-auth.services.api.unity.com/v1/authentication/anonymous');
    a.setRequestHeader('UnityEnvironment', 'development');
    a.send('');
    assert.strictEqual(a.sentTo, 'https://player-auth.services.api.unity.com/v1/authentication/anonymous');
    const bad = page('online');
    const b = new bad.ctx.XMLHttpRequest();
    b.open('POST', 'https://player-auth.services.api.unity.com/v1/authentication/anonymous');
    b.setRequestHeader('UnityEnvironment', 'production');
    b.send('');
    assert.strictEqual(b.sentTo, '/__uxtest/blocked');
    assert.ok(bad.state.stopped);
    const none = page('online');
    const c = new none.ctx.XMLHttpRequest();
    c.open('POST', 'https://player-auth.services.api.unity.com/v1/authentication/anonymous');
    c.send('');
    assert.strictEqual(c.sentTo, '/__uxtest/blocked');
    assert.ok(none.state.stopped, 'a missing UnityEnvironment header is a STOP');
  },
  'online: Cloud Save and unknown hosts are refused by default'() {
    const p = page('online');
    const x = new p.ctx.XMLHttpRequest();
    x.open('POST', 'https://cloud-save.services.api.unity.com/v1/data/projects/p/players/q/items');
    x.send('{}');
    assert.strictEqual(x.sentTo, '/__uxtest/blocked');
    assert.strictEqual(p.state.stopped, null, 'refused without a STOP');
  },
  'no body, token or Authorization header is ever posted to the log'() {
    const p = page('offline');
    const x = new p.ctx.XMLHttpRequest();
    x.open('POST', 'https://cloud-save.services.api.unity.com/v1/x?token=secret');
    x.setRequestHeader('Authorization', 'Bearer secret');
    x.send('{"secret":1}');
    const text = JSON.stringify(p.logs());
    assert.ok(text.indexOf('secret') < 0, text);
  },
};

(async () => {
  let failed = 0;
  for (const [name, fn] of Object.entries(tests)) {
    try { await fn(); console.log('ok   ' + name); } catch (e) { failed++; console.log('FAIL ' + name + '\n     ' + e.message); }
  }
  console.log(failed ? failed + ' failed' : 'all ' + Object.keys(tests).length + ' passed');
  process.exit(failed ? 1 : 0);
})();
