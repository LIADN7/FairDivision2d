#!/usr/bin/env python3
"""Tests for serve_test_build.verify_build with synthetic WebGL artifacts (no Unity build, no network).

  python -I -m unittest discover -s tools/webgl_test -p "test_*.py"
"""
import json
import os
import shutil
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import serve_test_build as stb  # noqa: E402

TEST_LITERALS = list(stb.REQUIRED_LITERALS)
RELEASE_LITERALS = list(stb.FORBIDDEN_LITERALS)
COMMON_LITERALS = ["production", "Unity Services initialized successfully"]


def metadata(literals, identifiers, version=31):
    header_size = 256
    data = b"".join(s.encode("utf-8") for s in literals)
    table, index = b"", 0
    for s in literals:
        n = len(s.encode("utf-8"))
        table += struct.pack("<Ii", n, index)
        index += n
    strings = b"\0".join(s.encode("utf-8") for s in identifiers) + b"\0"
    lit_off = header_size
    data_off = lit_off + len(table)
    str_off = data_off + len(data)
    header = struct.pack("<Ii6I", 0xFAB11BAF, version, lit_off, len(table), data_off, len(data), str_off, len(strings))
    return header.ljust(header_size, b"\0") + table + data + strings


def unity_web_data(files):
    names = list(files)
    header_size = 20 + sum(12 + len(n.encode("utf-8")) for n in names)
    header, body, offset = b"", b"", header_size
    for n in names:
        header += struct.pack("<III", offset, len(files[n]), len(n.encode("utf-8"))) + n.encode("utf-8")
        body += files[n]
        offset += len(files[n])
    return b"UnityWebData1.0\0" + struct.pack("<I", header_size) + header + body


def write_build(root, literals, identifiers, startup, compressed=False, framework="// include: shell.js\n"):
    os.makedirs(os.path.join(root, "Build"))
    with open(os.path.join(root, "index.html"), "w") as f:
        f.write("<html><head></head><body></body></html>")
    inits = {"root": [{"assemblyName": a, "className": c, "methodName": m} for a, c, m in startup]}
    data = unity_web_data({
        "RuntimeInitializeOnLoads.json": json.dumps(inits).encode(),
        "Il2CppData/Metadata/global-metadata.dat": metadata(literals, identifiers),
    })
    suffix = ".br" if compressed else ""
    for ext, content in ((".data", data), (".framework.js", framework.encode()), (".wasm", b"\0asm"),
                         (".loader.js", b"")):
        name = "b" + ext + (suffix if ext != ".loader.js" else "")
        with open(os.path.join(root, "Build", name), "wb") as f:
            f.write(content)


TEST_STARTUP = [("Assembly-CSharp", "TestModeBadge", "Create"), ("PhotonRealtime", "ConnectionHandler", "StaticReset")]


class VerifyBuildTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()
        self.build = os.path.join(self.dir, "uxtest_case")

    def tearDown(self):
        shutil.rmtree(self.dir, ignore_errors=True)

    def failed_checks(self):
        passed, rows, _ = stb.verify_build(self.build)
        return passed, [check for status, check, _ in rows if status == "FAIL"]

    def test_development_build_passes(self):
        write_build(self.build, TEST_LITERALS + COMMON_LITERALS, stb.REQUIRED_IDENTIFIERS + ["Launcher"], TEST_STARTUP)
        passed, failed = self.failed_checks()
        self.assertTrue(passed, failed)

    def test_release_build_under_test_label_fails(self):
        # What the incident produced: a Release player in a "uxtest"/Development-labelled folder.
        write_build(self.build, RELEASE_LITERALS + COMMON_LITERALS, ["Launcher"],
                    [("PhotonRealtime", "ConnectionHandler", "StaticReset")])
        passed, failed = self.failed_checks()
        self.assertFalse(passed)
        self.assertIn("Test-only startup code compiled", failed)
        self.assertIn("Release literal absent: 'Redirecting to survey URL: '", failed)
        self.assertIn("Test literal present: 'TEST MODE: Photon AppVersion = '", failed)

    def test_release_literal_alone_fails(self):
        write_build(self.build, TEST_LITERALS + ["Redirecting to survey URL: "], stb.REQUIRED_IDENTIFIERS, TEST_STARTUP)
        passed, failed = self.failed_checks()
        self.assertFalse(passed)
        self.assertEqual(failed, ["Release literal absent: 'Redirecting to survey URL: '"])

    def test_missing_environment_literal_fails(self):
        literals = [s for s in TEST_LITERALS if s != stb.TEST_ENVIRONMENT]
        write_build(self.build, literals, stb.REQUIRED_IDENTIFIERS, TEST_STARTUP)
        passed, failed = self.failed_checks()
        self.assertFalse(passed)
        self.assertEqual(failed, ["Test literal present: 'development'"])

    def test_compressed_build_is_refused(self):
        write_build(self.build, TEST_LITERALS, stb.REQUIRED_IDENTIFIERS, TEST_STARTUP, compressed=True)
        passed, failed = self.failed_checks()
        self.assertFalse(passed)
        self.assertEqual(failed, ["Uncompressed build (inspectable)"])

    def test_not_a_build_folder_fails(self):
        os.makedirs(self.build)
        passed, failed = self.failed_checks()
        self.assertFalse(passed)
        self.assertEqual(failed, ["WebGL build folder"])

    def test_minified_framework_is_only_informational(self):
        write_build(self.build, TEST_LITERALS, stb.REQUIRED_IDENTIFIERS, TEST_STARTUP, framework="var a=1;")
        passed, failed = self.failed_checks()
        self.assertTrue(passed, failed)

    def test_csp_offline_allows_only_self(self):
        csp = stb.csp_for("offline", False)
        self.assertIn("connect-src 'self' blob: data:;", csp)
        self.assertNotIn("unity.com", csp)
        self.assertNotIn("photon", csp)

    def test_csp_online_cloud_save_only_when_allowed(self):
        self.assertNotIn("cloud-save", stb.csp_for("online", False))
        self.assertIn("https://cloud-save.services.api.unity.com", stb.csp_for("online", True))


if __name__ == "__main__":
    unittest.main()
