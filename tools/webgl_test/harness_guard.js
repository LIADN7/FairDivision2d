// TEST HARNESS ONLY. Injected first into every page by tools/webgl_test/serve_test_build.py (local 127.0.0.1 server).
// Records every attempted connection and the TEST MODE console lines, and enforces the connection policy:
//   offline: no request leaves the page (only same-origin files).
//   online:  Photon only after "TEST MODE: Photon AppVersion = <test AppVersion>" was logged and only while its
//            frames carry that AppVersion; Unity Authentication only with "UnityEnvironment: <test environment>";
//            Cloud Save only when the server allows it; nothing else. Any violation is a STOP (fail-closed).
// Never records request bodies, tokens or Authorization headers.
(function () {
  'use strict';
  var MODE = '__UXTEST_MODE__';
  var POLICY = __UXTEST_POLICY__;
  var origFetch = window.fetch ? window.fetch.bind(window) : null;
  var OrigWebSocket = window.WebSocket;
  var origOpen = XMLHttpRequest.prototype.open;
  var origSetHeader = XMLHttpRequest.prototype.setRequestHeader;
  var origSend = XMLHttpRequest.prototype.send;
  var origBeacon = navigator.sendBeacon ? navigator.sendBeacon.bind(navigator) : null;
  var state = window.__uxtest = {
    mode: MODE, policy: POLICY, appVersion: null, appVersionOk: false, servicesLine: null, authEnvironment: null,
    wireAppVersion: null, stopped: null, attempts: [], console: [], sockets: []
  };

  function post(entry) {
    entry.pageTime = new Date().toISOString();
    try {
      var body = JSON.stringify(entry);
      if (origFetch) origFetch('/__uxtest/log', { method: 'POST', body: body, keepalive: true }).catch(function () {});
    } catch (e) { /* never break the page */ }
  }

  function parse(url) {
    try { return new URL(String(url), location.href); } catch (e) { return null; }
  }

  function isLocal(u) {
    return !u || u.protocol === 'blob:' || u.protocol === 'data:' || u.origin === location.origin;
  }

  function hostMatches(host, list, suffix) {
    host = (host || '').toLowerCase();
    for (var i = 0; i < list.length; i++) {
      if (suffix ? host.slice(-list[i].length) === list[i] : host === list[i]) return true;
    }
    return false;
  }

  function headerValue(headers, name) {
    if (!headers) return null;
    name = name.toLowerCase();
    if (typeof headers.get === 'function') return headers.get(name);
    for (var k in headers) { if (Object.prototype.hasOwnProperty.call(headers, k) && k.toLowerCase() === name) return headers[k]; }
    return null;
  }

  function stop(reason) {
    if (state.stopped) return;
    state.stopped = reason;
    post({ kind: 'STOP', reason: reason });
    try { console.error('UXTEST HARNESS STOP: ' + reason); } catch (e) {}
    state.sockets.forEach(function (s) { try { s.close(); } catch (e) {} });
    var show = function () {
      var d = document.createElement('div');
      d.id = 'uxtest-stop';
      d.textContent = 'UXTEST HARNESS STOP - ' + reason + ' - all connections closed. Close this tab.';
      d.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:rgba(160,0,0,.92);color:#fff;' +
        'font:bold 22px sans-serif;display:flex;align-items:center;justify-content:center;text-align:center;padding:40px';
      document.body.appendChild(d);
    };
    if (document.body) show(); else document.addEventListener('DOMContentLoaded', show);
  }

  // Decide a request: returns null to allow, or the reason it is refused.
  function decide(kind, u, headers) {
    var env = headerValue(headers, 'UnityEnvironment');
    var attempt = { kind: 'attempt', via: kind, url: u ? u.protocol + '//' + u.host + u.pathname : '(unparsable)' };
    if (env !== null && env !== undefined) attempt.UnityEnvironment = env;
    var reason = null;
    var host = u ? u.hostname : '';
    if (state.stopped) reason = 'stopped';
    else if (MODE !== 'online') reason = 'offline mode';
    else if (hostMatches(host, POLICY.photonSuffixes, true)) {
      if (kind !== 'websocket') reason = 'Photon only over WebSocket';
      else if (!state.appVersionOk) { reason = 'Photon before TEST MODE AppVersion ' + POLICY.appVersion + ' was confirmed'; stop(reason); }
    } else if (hostMatches(host, POLICY.authHosts, false)) {
      if (env !== POLICY.environment) { reason = 'Unity Authentication with UnityEnvironment=' + env; stop(reason); }
      else state.authEnvironment = env;
    } else if (hostMatches(host, POLICY.cloudSaveHosts, false)) {
      if (state.authEnvironment !== POLICY.environment) reason = 'Cloud Save before a ' + POLICY.environment + ' sign-in';
    } else reason = 'host not needed for the test';
    attempt.decision = reason ? 'BLOCKED: ' + reason : 'allowed';
    state.attempts.push(attempt);
    post(attempt);
    return reason;
  }

  // --- console: effective runtime configuration --------------------------------------------------------------
  var appVersionRe = /TEST MODE: Photon AppVersion = (\S+)/;
  ['log', 'info', 'warn', 'error'].forEach(function (level) {
    var orig = console[level].bind(console);
    console[level] = function () {
      try {
        var text = Array.prototype.map.call(arguments, String).join(' ');
        var m = appVersionRe.exec(text);
        if (m) {
          state.appVersion = m[1];
          if (m[1] === POLICY.appVersion) state.appVersionOk = true;
          else stop('Photon AppVersion is ' + m[1] + ', expected ' + POLICY.appVersion);
        }
        if (text.indexOf('TEST MODE: Unity Services environment') >= 0) state.servicesLine = text.trim();
        state.console.push(new Date().toISOString() + ' ' + level + ': ' + text.slice(0, 500));
        if (state.console.length > 4000) state.console.shift();
        if (/TEST MODE|AppVersion|Unity Services|Authentication|Cloud|Photon|Exception|error/i.test(text) || level === 'error') {
          post({ kind: 'console', level: level, text: text.slice(0, 500) });
        }
      } catch (e) { /* never break logging */ }
      return orig.apply(null, arguments);
    };
  });

  // --- fetch ----------------------------------------------------------------------------------------------------
  if (origFetch) {
    window.fetch = function (input, init) {
      var url = (input && typeof input === 'object' && 'url' in input) ? input.url : input;
      var u = parse(url);
      if (isLocal(u)) return origFetch(input, init);
      var headers = (init && init.headers) || (input && typeof input === 'object' ? input.headers : null);
      var reason = decide('fetch', u, headers);
      if (reason) return Promise.reject(new TypeError('Failed to fetch (uxtest harness: ' + reason + ')'));
      return origFetch(input, init);
    };
  }

  // --- XMLHttpRequest (the decision is made at send(), when the headers are known) -------------------------------
  XMLHttpRequest.prototype.open = function (method, url) {
    this.__ux = { method: method, url: parse(url), headers: {}, async: arguments.length < 3 || arguments[2] !== false };
    return origOpen.apply(this, arguments);
  };
  XMLHttpRequest.prototype.setRequestHeader = function (name, value) {
    if (this.__ux) this.__ux.headers[name] = value;
    return origSetHeader.apply(this, arguments);
  };
  XMLHttpRequest.prototype.send = function (body) {
    var ux = this.__ux;
    if (ux && !isLocal(ux.url)) {
      var reason = decide('xhr', ux.url, ux.headers);
      if (reason) {
        // Re-target to the local server, which answers 451; the original request is never sent.
        origOpen.call(this, ux.method || 'GET', '/__uxtest/blocked', ux.async);
        return origSend.call(this, null);
      }
    }
    return origSend.apply(this, arguments);
  };

  // --- sendBeacon -----------------------------------------------------------------------------------------------
  if (origBeacon) {
    navigator.sendBeacon = function (url, data) {
      var u = parse(url);
      if (isLocal(u)) return origBeacon(url, data);
      return decide('beacon', u, null) ? false : origBeacon(url, data);
    };
  }

  // --- WebSocket ------------------------------------------------------------------------------------------------
  function bytesOf(data) {
    if (typeof data === 'string') return null;
    if (data instanceof ArrayBuffer) return new Uint8Array(data);
    if (ArrayBuffer.isView(data)) return new Uint8Array(data.buffer, data.byteOffset, data.byteLength);
    return null;
  }
  function latin1(bytes) {
    var s = '';
    for (var i = 0; i < bytes.length; i += 8192) s += String.fromCharCode.apply(null, bytes.subarray(i, i + 8192));
    return s;
  }
  var wireRe = new RegExp('([A-Za-z0-9.\\-]*)_' + POLICY.punVersion.replace('.', '\\.'), 'g');

  function FakeClosedSocket(url, reason) {
    var self = this;
    this.url = url; this.readyState = 3; this.protocol = ''; this.extensions = ''; this.bufferedAmount = 0;
    this.binaryType = 'blob'; this.onopen = this.onmessage = this.onerror = this.onclose = null;
    var listeners = {};
    this.addEventListener = function (t, f) { (listeners[t] = listeners[t] || []).push(f); };
    this.removeEventListener = function (t, f) { listeners[t] = (listeners[t] || []).filter(function (g) { return g !== f; }); };
    this.send = function () {}; this.close = function () {};
    setTimeout(function () {
      var err = { type: 'error', target: self };
      var close = { type: 'close', code: 1006, reason: 'uxtest harness: ' + reason, wasClean: false, target: self };
      if (self.onerror) self.onerror(err);
      (listeners.error || []).forEach(function (f) { f(err); });
      if (self.onclose) self.onclose(close);
      (listeners.close || []).forEach(function (f) { f(close); });
    }, 0);
  }

  function GuardedWebSocket(url, protocols) {
    var u = parse(url);
    if (!isLocal(u)) {
      var reason = decide('websocket', u, null);
      if (reason) return new FakeClosedSocket(String(url), reason);
    }
    var ws = protocols === undefined ? new OrigWebSocket(url) : new OrigWebSocket(url, protocols);
    if (!isLocal(u)) {
      state.sockets.push(ws);
      var realSend = ws.send.bind(ws);
      ws.send = function (data) {
        if (state.stopped) { try { ws.close(); } catch (e) {} return; }
        var bytes = bytesOf(data);
        if (bytes) {
          var text = latin1(bytes), m;
          wireRe.lastIndex = 0;
          while ((m = wireRe.exec(text)) !== null) {
            var seen = m[1] + '_' + POLICY.punVersion;
            if (seen === POLICY.appVersion) {
              if (state.wireAppVersion !== seen) { state.wireAppVersion = seen; post({ kind: 'wire', appVersion: seen, host: u.host }); }
            } else if (/^[A-Za-z0-9.\-]{0,32}$/.test(m[1])) {
              stop('outgoing Photon frame carries AppVersion "' + seen + '", expected ' + POLICY.appVersion);
              return;
            }
          }
        }
        return realSend(data);
      };
    }
    return ws;
  }
  GuardedWebSocket.prototype = OrigWebSocket.prototype;
  ['CONNECTING', 'OPEN', 'CLOSING', 'CLOSED'].forEach(function (k) { GuardedWebSocket[k] = OrigWebSocket[k]; });
  window.WebSocket = GuardedWebSocket;

  // Test automation handle: window.__unityInstance (e.g. __unityInstance.SendMessage('NameField', 'set_text', 'QA')),
  // captured when the loader script finishes loading, before the page's own onload handler calls createUnityInstance.
  document.addEventListener('load', function (e) {
    var t = e.target;
    if (!t || t.tagName !== 'SCRIPT' || typeof window.createUnityInstance !== 'function' || window.createUnityInstance.__ux) return;
    var origCreate = window.createUnityInstance;
    window.createUnityInstance = function () {
      return origCreate.apply(this, arguments).then(function (instance) { window.__unityInstance = instance; return instance; });
    };
    window.createUnityInstance.__ux = true;
  }, true);

  document.addEventListener('securitypolicyviolation', function (e) {
    post({ kind: 'csp-event', blockedURI: e.blockedURI, directive: e.violatedDirective });
  });
  post({ kind: 'harness', mode: MODE, appVersion: POLICY.appVersion, environment: POLICY.environment });
})();
