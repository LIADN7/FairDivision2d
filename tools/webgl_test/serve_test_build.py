#!/usr/bin/env python3
"""Verify, then serve, a local WebGL Development (TEST MODE) build of Fair Division 2D. Fail-closed.

Test isolation exists only in Development builds (#if UNITY_EDITOR || DEVELOPMENT_BUILD): Photon AppVersion
"uxtest_2.42", Unity Services environment "development" with sign-in profile "uxtest", no survey redirect, TEST MODE
badge. A Release player built under a "Development" or "uxtest" label connects to PRODUCTION. This script refuses to
serve a build unless the artifact itself proves it is a test build, and it serves only on 127.0.0.1.

  python -I tools/webgl_test/serve_test_build.py Builds/uxtest_x --check-only     # offline artifact checks only
  python -I tools/webgl_test/serve_test_build.py Builds/uxtest_x --mode offline   # serve; every network call blocked
  python -I tools/webgl_test/serve_test_build.py Builds/uxtest_x --mode online    # serve; Photon + Unity Auth only,
                                                                                  # each checked at runtime

1. Artifact checks (always; any FAIL = exit 2, nothing served). They read the compiled build, not its folder name:
   the IL2CPP metadata inside <name>.data (string literals and identifiers of the compiled C# code) and the list of
   startup methods (RuntimeInitializeOnLoads.json). Test-only literals must be present and release-only literals
   (the #else branches: survey redirect, "0.0.1" lobby AppVersion) absent. Compressed builds cannot be inspected and
   are refused: build with the "Web - Desktop - Development" profile (compression disabled).

2. Runtime harness (served pages only): a guard script is injected first into every HTML page, and every response
   carries a Content-Security-Policy that limits connections. It records each attempted connection (host, path,
   the UnityEnvironment request header; never bodies or tokens) and every TEST MODE console line, and posts them
   to this server, which prints them and appends them to <build folder>.harness.jsonl.
   --mode offline: every non-local request is refused by the guard and by the CSP (connect-src 'self'). Use it
     first: it shows the effective runtime configuration and every service the build tries to reach, with no
     connection.
   --mode online: allowed only after the offline run passed. A Photon WebSocket opens only after the page logged
     "TEST MODE: Photon AppVersion = uxtest_2.42"; outgoing Photon frames that carry another "..._2.42" AppVersion
     stop the run. Unity Authentication requests pass only with "UnityEnvironment: development". Cloud Save is
     refused unless --allow-cloud-save; everything else is refused. Any violation is a STOP: all sockets are
     closed, every later request is refused and a red banner covers the page.
     Production and test share the Photon App ID and the Unity Services domains, so the host allowlist alone proves
     nothing; the AppVersion and UnityEnvironment checks are the logical-environment gate.

Standard library only (Python 3.8+). Contains no credentials. Docs: docs/PROJECT_CONTEXT.md section 13.2.
"""
import argparse
import datetime
import http.server
import json
import os
import re
import struct
import sys
import threading
import urllib.parse

HERE = os.path.dirname(os.path.abspath(__file__))
PUN_VERSION = "2.42"
TEST_APP_VERSION = "uxtest_" + PUN_VERSION
TEST_ENVIRONMENT = "development"

# Literals that exist only in the test branches (#if UNITY_EDITOR || DEVELOPMENT_BUILD) of the game code.
REQUIRED_LITERALS = {
    "uxtest": "Launcher.TestGameVersion (Photon test AppVersion prefix) / DatabaseManager sign-in profile",
    "TEST MODE: Photon AppVersion = ": "Launcher test branch",
    "TEST MODE: Unity Services environment = development, profile = uxtest": "DatabaseManager test branch",
    TEST_ENVIRONMENT: "Unity Services test environment name (SetEnvironmentName)",
    "TEST MODE: survey redirect suppressed (status=": "Config.RedirectToSurvey test branch",
    "TEST MODE - not research data": "TestModeBadge",
}
# Literals that exist only in the release branches (#else) - they must be compiled out of a test build.
FORBIDDEN_LITERALS = {
    "Redirecting to survey URL: ": "Config.RedirectToSurvey release branch (opens the survey panel)",
    "0.0.1": "Launcher release branch (PhotonNetwork.GameVersion before connecting)",
}
REQUIRED_IDENTIFIERS = ["TestModeBadge", "TestGameVersion", "TestReadBackAsync"]

PHOTON_HOST_SUFFIXES = (".photonengine.io", ".exitgames.com", ".photonengine.com")
AUTH_HOSTS = ("player-auth.services.api.unity.com",)
CLOUD_SAVE_HOSTS = ("cloud-save.services.api.unity.com",)

MIME = {
    ".html": "text/html; charset=utf-8", ".js": "application/javascript", ".wasm": "application/wasm",
    ".data": "application/octet-stream", ".json": "application/json", ".css": "text/css", ".png": "image/png",
    ".ico": "image/x-icon", ".jpg": "image/jpeg", ".svg": "image/svg+xml", ".txt": "text/plain; charset=utf-8",
}
COMPRESSED_SUFFIXES = (".br", ".gz", ".unityweb")


# ---------------------------------------------------------------------------------------------------------------
# 1. Artifact checks
# ---------------------------------------------------------------------------------------------------------------

def read_unity_web_data(path):
    """Files packed in a WebGL <name>.data (UnityWebData1.0): {name: bytes}."""
    with open(path, "rb") as f:
        blob = f.read()
    if not blob.startswith(b"UnityWebData1.0\0"):
        raise ValueError("not a UnityWebData1.0 package (compressed or not a WebGL data file)")
    header_size = struct.unpack_from("<I", blob, 16)[0]
    pos, files = 20, {}
    while pos < header_size:
        offset, size, name_len = struct.unpack_from("<III", blob, pos)
        pos += 12
        name = blob[pos:pos + name_len].decode("utf-8")
        pos += name_len
        files[name] = blob[offset:offset + size]
    return files


def read_il2cpp_metadata(meta):
    """String literals and identifier strings of IL2CPP global-metadata.dat (header layout of versions 24-31)."""
    sanity, version = struct.unpack_from("<Ii", meta, 0)
    if sanity != 0xFAB11BAF:
        raise ValueError("global-metadata.dat has a wrong sanity value")
    lit_off, lit_size, data_off, data_size, str_off, str_size = struct.unpack_from("<6I", meta, 8)
    literals = set()
    for i in range(lit_size // 8):
        length, index = struct.unpack_from("<Ii", meta, lit_off + i * 8)
        literals.add(meta[data_off + index:data_off + index + length].decode("utf-8", "replace"))
    identifiers = set(s.decode("utf-8", "replace") for s in meta[str_off:str_off + str_size].split(b"\0"))
    return version, literals, identifiers


def verify_build(build_dir):
    """Returns (passed, rows, info). rows: (status, check, detail); status PASS / FAIL / INFO."""
    rows, info = [], {}

    def add(ok, check, detail):
        rows.append(("PASS" if ok else "FAIL", check, detail))
        return ok

    build_sub = os.path.join(build_dir, "Build")
    if not add(os.path.isfile(os.path.join(build_dir, "index.html")) and os.path.isdir(build_sub),
               "WebGL build folder", build_dir):
        return False, rows, info
    names = os.listdir(build_sub)
    compressed = [n for n in names if n.endswith(COMPRESSED_SUFFIXES)]
    if not add(not compressed, "Uncompressed build (inspectable)",
               "compressed files: %s - build with the Development profile (compression disabled)" % compressed
               if compressed else "no .br/.gz/.unityweb files"):
        return False, rows, info
    loaders = [n for n in names if n.endswith(".loader.js")]
    if not add(len(loaders) == 1, "One loader", ", ".join(loaders) or "none"):
        return False, rows, info
    stem = loaders[0][:-len(".loader.js")]
    info["name"] = stem
    paths = {k: os.path.join(build_sub, stem + k) for k in (".data", ".framework.js", ".wasm")}
    missing = [os.path.basename(p) for p in paths.values() if not os.path.isfile(p)]
    if not add(not missing, "Build files present", "missing: %s" % missing if missing else stem + ".data/.framework.js/.wasm"):
        return False, rows, info

    try:
        packed = read_unity_web_data(paths[".data"])
        meta = packed["Il2CppData/Metadata/global-metadata.dat"]
        inits = json.loads(packed["RuntimeInitializeOnLoads.json"].decode("utf-8"))
        version, literals, identifiers = read_il2cpp_metadata(meta)
    except (ValueError, KeyError, struct.error) as e:
        add(False, "Read compiled code (IL2CPP metadata)", "cannot read %s: %s" % (os.path.basename(paths[".data"]), e))
        return False, rows, info
    add(True, "Read compiled code (IL2CPP metadata)", "metadata v%d, %d string literals" % (version, len(literals)))

    startup = [(r.get("assemblyName"), r.get("className"), r.get("methodName")) for r in inits.get("root", [])]
    add(("Assembly-CSharp", "TestModeBadge", "Create") in startup, "Test-only startup code compiled",
        "RuntimeInitializeOnLoads lists Assembly-CSharp TestModeBadge.Create (#if DEVELOPMENT_BUILD only)")
    for literal, where in REQUIRED_LITERALS.items():
        add(literal in literals, "Test literal present: %r" % literal, where)
    for literal, where in FORBIDDEN_LITERALS.items():
        add(literal not in literals, "Release literal absent: %r" % literal, where)
    for ident in REQUIRED_IDENTIFIERS:
        add(ident in identifiers, "Test identifier compiled: %s" % ident, "IL2CPP metadata identifiers")

    with open(paths[".framework.js"], "r", encoding="utf-8", errors="replace") as f:
        head = f.read(4096)
    rows.append(("PASS" if "// include: shell.js" in head else "INFO", "Development runtime (unminified framework.js)",
                 "supporting evidence only; the decisive evidence is the DEVELOPMENT_BUILD-only code above"))

    sa = os.path.join(build_dir, "StreamingAssets", "UnityServicesProjectConfiguration.json")
    if os.path.isfile(sa):
        try:
            with open(sa, "r", encoding="utf-8") as f:
                cfg = json.load(f)
            env = dict(zip(cfg.get("Keys", []), [v.get("m_Value") if isinstance(v, dict) else v
                                                  for v in cfg.get("Values", [])])).get(
                "com.unity.services.core.environment-name")
            rows.append(("INFO", "Embedded Unity Services default environment",
                         "%r (the same in Release; the test code overrides it with %r at runtime - confirm the "
                         "UnityEnvironment header with --mode offline)" % (env, TEST_ENVIRONMENT)))
        except (ValueError, OSError) as e:
            rows.append(("INFO", "Embedded Unity Services default environment", "unreadable: %s" % e))
    passed = all(status != "FAIL" for status, _, _ in rows)
    return passed, rows, info


def print_rows(rows, passed):
    for status, check, detail in rows:
        print("  [%s] %s - %s" % (status, check, detail))
    print("ARTIFACT CHECK: %s" % ("PASS" if passed else "FAIL - do not serve, do not connect"))


# ---------------------------------------------------------------------------------------------------------------
# 2. Local server with the runtime harness
# ---------------------------------------------------------------------------------------------------------------

def csp_for(mode, allow_cloud_save):
    connect = ["'self'", "blob:", "data:"]
    if mode == "online":
        connect += ["wss://*%s:*" % s for s in PHOTON_HOST_SUFFIXES]
        connect += ["https://%s" % h for h in AUTH_HOSTS]
        if allow_cloud_save:
            connect += ["https://%s" % h for h in CLOUD_SAVE_HOSTS]
    return ("default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval' blob:; "
            "style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; "
            "media-src 'self' data: blob:; worker-src 'self' blob:; frame-src 'none'; object-src 'none'; "
            "form-action 'none'; connect-src %s; report-uri /__uxtest/csp" % " ".join(connect))


def make_handler(root, mode, allow_cloud_save, log_path):
    with open(os.path.join(HERE, "harness_guard.js"), "r", encoding="utf-8") as f:
        guard_js = (f.read()
                    .replace("__UXTEST_MODE__", mode)
                    .replace("__UXTEST_POLICY__", json.dumps({
                        "photonSuffixes": PHOTON_HOST_SUFFIXES, "authHosts": AUTH_HOSTS,
                        "cloudSaveHosts": CLOUD_SAVE_HOSTS if allow_cloud_save else [],
                        "appVersion": TEST_APP_VERSION, "punVersion": PUN_VERSION,
                        "environment": TEST_ENVIRONMENT})))
    csp = csp_for(mode, allow_cloud_save)
    lock = threading.Lock()
    root_real = os.path.realpath(root)

    def record(entry):
        entry.setdefault("serverTime", datetime.datetime.now(datetime.timezone.utc).isoformat())
        line = json.dumps(entry, ensure_ascii=False)
        with lock:
            with open(log_path, "a", encoding="utf-8") as f:
                f.write(line + "\n")
        kind = entry.get("kind", "?")
        if kind == "console":
            print("[console] %s" % entry.get("text", "")[:300])
        elif kind == "csp":
            print("[CSP BLOCKED] %s (%s)" % (entry.get("blocked-uri"), entry.get("violated-directive")))
        else:
            print("[%s] %s" % (kind, json.dumps({k: v for k, v in entry.items() if k not in ("kind", "serverTime")},
                                               ensure_ascii=False)[:400]))
        sys.stdout.flush()

    class Handler(http.server.BaseHTTPRequestHandler):
        server_version = "uxtest-local"

        def log_message(self, fmt, *args):  # quiet: the harness log is the record
            pass

        def _send(self, code, body, ctype="text/plain; charset=utf-8"):
            self.send_response(code)
            self.send_header("Content-Type", ctype)
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Security-Policy", csp)
            self.end_headers()
            if self.command != "HEAD":
                self.wfile.write(body)

        def do_HEAD(self):
            self.do_GET()

        def do_GET(self):
            path = urllib.parse.unquote(urllib.parse.urlsplit(self.path).path)
            if path == "/__uxtest/guard.js":
                return self._send(200, guard_js.encode("utf-8"), MIME[".js"])
            if path.startswith("/__uxtest/"):
                return self._send(451, b"blocked by the uxtest harness")
            rel = path.lstrip("/") or "index.html"
            full = os.path.realpath(os.path.join(root_real, rel))
            if os.path.isdir(full):
                full = os.path.join(full, "index.html")
            if not (full == root_real or full.startswith(root_real + os.sep)) or not os.path.isfile(full):
                return self._send(404, b"not found")
            if full.endswith(COMPRESSED_SUFFIXES):
                return self._send(403, b"compressed files are not served by the test server")
            ext = os.path.splitext(full)[1].lower()
            with open(full, "rb") as f:
                body = f.read()
            if ext == ".html":
                text = body.decode("utf-8", "replace")
                m = re.search(r"<head[^>]*>", text, re.I)
                if not m:
                    return self._send(500, b"refused: page without <head>, the harness guard cannot be injected")
                text = text[:m.end()] + '\n<script src="/__uxtest/guard.js"></script>' + text[m.end():]
                body = text.encode("utf-8")
            self._send(200, body, MIME.get(ext, "application/octet-stream"))

        def do_POST(self):
            path = urllib.parse.urlsplit(self.path).path
            length = min(int(self.headers.get("Content-Length") or 0), 1 << 20)
            raw = self.rfile.read(length) if length else b""
            if path == "/__uxtest/log":
                try:
                    entry = json.loads(raw.decode("utf-8"))
                    if isinstance(entry, dict):
                        record(entry)
                except ValueError:
                    pass
                return self._send(204, b"")
            if path == "/__uxtest/csp":
                try:
                    report = json.loads(raw.decode("utf-8")).get("csp-report", {})
                    record({"kind": "csp", "blocked-uri": report.get("blocked-uri"),
                            "violated-directive": report.get("violated-directive")})
                except ValueError:
                    pass
                return self._send(204, b"")
            return self._send(451, b"blocked by the uxtest harness")

    return Handler


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("build_dir")
    ap.add_argument("--check-only", action="store_true", help="run the artifact checks and exit")
    ap.add_argument("--mode", choices=("offline", "online"), default="offline")
    ap.add_argument("--port", type=int, default=8765)
    ap.add_argument("--allow-cloud-save", action="store_true",
                    help="online mode: also allow Cloud Save requests (development environment only)")
    args = ap.parse_args()

    build_dir = os.path.abspath(args.build_dir)
    passed, rows, info = verify_build(build_dir)
    print("Build: %s" % build_dir)
    print_rows(rows, passed)
    if not passed:
        return 2
    if args.check_only:
        return 0

    log_path = build_dir.rstrip("\\/") + ".harness.jsonl"
    handler = make_handler(build_dir, args.mode, args.allow_cloud_save, log_path)
    server = http.server.ThreadingHTTPServer(("127.0.0.1", args.port), handler)
    print("Serving %s on http://127.0.0.1:%d/ in %s mode%s. Harness log: %s" % (
        info.get("name"), args.port, args.mode.upper(),
        " (Cloud Save allowed)" if args.allow_cloud_save and args.mode == "online" else "", log_path))
    print("CSP: %s" % csp_for(args.mode, args.allow_cloud_save))
    sys.stdout.flush()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
