#!/usr/bin/env python3
"""Focused tests for cc_export.py with synthetic records only (no participant data, no network).

Run:  python tools/research_export/test_cc_export.py
"""
import csv
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cc_export  # noqa: E402

REGIONS = cc_export.REGIONS
P1_VALUES = [30.0, 25.0, 25.0, 20.0]   # own values of regions 0-3 for Player 1 (sum 100)
P2_VALUES = [10.0, 40.0, 20.0, 30.0]   # own values of regions 0-3 for Player 2
CELLS = [120, 110, 130, 114]


def schedule(drawn):
    return ["Normal", "Normal", drawn, drawn, "InfoAChat", "InfoAChat"]


def round_record(run, role, r, scenario_type, owner):
    own, other = (P1_VALUES, P2_VALUES) if role == 1 else (P2_VALUES, P1_VALUES)
    chosen = [0, 3] if role == 1 else [1, 2]   # 1-2-1: P1 first pick + remainder, P2 two picks
    return {
        "SchemaVersion": 2, "RecordType": "round", "RunId": run, "RunIdShared": True, "PlayerRole": role,
        "UnityPlayerId": owner, "RoomName": "Room_1", "userID": "TEST_u%d" % role, "surveyID": "TEST_s",
        "ScenarioNum": r, "ScenarioType": scenario_type, "PlayerName": "QA-%d" % role, "otherPlayerName": "QA-%d" % (3 - role),
        "ChatHistory": [{"Message": "hi", "PlayerName": "QA-1", "Date": "2026-11-13T10:00:00+02:00"}]
        if "Chat" in scenario_type else [],
        "isAgreed": False, "PlayerCutValues": {"Red": 50.0, "Green": 50.0},
        "PlayerColorValues": dict(zip(REGIONS, own)), "OtherPlayerColorValues": dict(zip(REGIONS, other)),
        "AmountOfColor": dict(zip(REGIONS, CELLS)), "PlayerStartTime": "2026-11-13T10:00:00+02:00",
        "PlayerTimeCut": "2026-11-13T10:00:30+02:00", "CountPlayerSeeOther": 1 if "Info" in scenario_type else 0,
        "PlayerChoosenColors": chosen, "SumOfMyPart": sum(own[i] for i in chosen), "playerCutSquares": {},
    }


def final_record(run, role, owner, win_index, round_keys):
    x = sum((P1_VALUES if role == 1 else P2_VALUES)[i] for i in ([0, 3] if role == 1 else [1, 2]))
    return {"SchemaVersion": 2, "RecordType": "final", "RunId": run, "RunIdShared": True, "PlayerRole": role,
            "UnityPlayerId": owner, "RoomName": "Room_1", "SurveyID": "TEST_s", "UserID": "TEST_u%d" % role,
            "Name": "QA-%d" % role, "Email": "", "PhoneNumber": "", "ID": "", "WinRandIndex": win_index,
            "Winnings": cc_export.winnings_formula(x), "RoundKeys": round_keys}


def readable_round_key(run, role, r, t):
    return "CutAChoose_%d_%s_%s_P%d" % (r, t, run, role)


def cc_round_key(run, role, r, _t):
    return "CC_%s_P%d_R%d" % (run, role, r)


def add_run(items_by_role, run, drawn, key_style, rounds=range(6), final_roles=(1, 2), owners=("player-A", "player-B")):
    """Adds one experiment for both roles into items_by_role {role: {key: value}}."""
    make_key = readable_round_key if key_style == "readable" else cc_round_key
    for role in (1, 2):
        owner = owners[role - 1]
        keys = []
        for r in rounds:
            k = make_key(run, role, r, schedule(drawn)[r])
            items_by_role[role][k] = round_record(run, role, r, schedule(drawn)[r], owner)
            keys.append(k)
        if role in final_roles:
            fk = ("PlayerData_%s_P%d" % (run, role)) if key_style == "readable" else ("CC_%s_P%d_final" % (run, role))
            items_by_role[role][fk] = final_record(run, role, owner, 2, keys)


class ExportTests(unittest.TestCase):
    def export(self, docs):
        """docs: list of (filename, json document). Returns dict of CSV name -> list of row dicts."""
        with tempfile.TemporaryDirectory() as tmp:
            paths = []
            for name, doc in docs:
                p = os.path.join(tmp, name)
                with open(p, "w", encoding="utf-8") as f:
                    json.dump(doc, f)
                paths.append(p)
            out = os.path.join(tmp, "out")
            self.assertEqual(cc_export.main([out] + paths), 0)
            res = {}
            for n in ("experiments", "players", "rounds", "legacy_keys", "unknown_keys"):
                with open(os.path.join(out, n + ".csv"), encoding="utf-8") as f:
                    res[n] = list(csv.DictReader(f))
            return res

    def runs(self, res):
        return {r["RunId"]: r for r in res["experiments"]}

    def test_readable_complete_info_and_chat(self):
        items = {1: {}, 2: {}}
        add_run(items, "20261113-100000-aaaaaa", "Info", "readable")
        add_run(items, "20261113-110000-bbbbbb", "Chat", "readable")
        res = self.export([("player-A.json", items[1]), ("player-B.json", items[2])])
        runs = self.runs(res)
        for run in runs.values():
            self.assertEqual(run["Complete"], "True", run["Issues"])
            self.assertEqual(run["KeyFormats"], "readable")
        rows = [r for r in res["rounds"] if r["RunId"] == "20261113-110000-bbbbbb" and r["ScenarioNum"] in ("2", "3")]
        self.assertEqual({r["ScenarioType"] for r in rows}, {"Chat"})
        self.assertTrue(all(r["Key"].startswith("CutAChoose_" + r["ScenarioNum"] + "_Chat_") for r in rows))
        self.assertTrue(all(r["KeyScenarioCheck"] == "ok" for r in res["rounds"]))
        self.assertTrue(all(p["WinningsCheck"] == "ok" and p["FinalKey"].startswith("PlayerData_") for p in res["players"]))

    def test_cc_and_readable_and_legacy_mixed(self):
        items = {1: {}, 2: {}}
        add_run(items, "20261112-100000-cccccc", "Info", "cc")
        add_run(items, "20261113-120000-dddddd", "Chat", "readable")
        # schema 1 keys: fixed names, JSON string values, no run ID -> legacy only
        items[1]["CutAChoose_3_Chat"] = json.dumps({"ScenarioNum": 3, "ScenarioType": "Chat"})
        items[1]["PlayerData"] = json.dumps({"Winnings": 30})
        # input layouts: read-back document for A, REST "results" layout for B
        doc_a = {"playerId": "player-A", "allKeys": [{"key": k} for k in items[1]], "items": items[1]}
        doc_b = {"results": [{"key": k, "value": v} for k, v in items[2].items()]}
        res = self.export([("player-A.json", doc_a), ("player-B.json", doc_b)])
        runs = self.runs(res)
        self.assertEqual(set(runs), {"20261112-100000-cccccc", "20261113-120000-dddddd"})
        self.assertEqual(runs["20261112-100000-cccccc"]["Complete"], "True", runs["20261112-100000-cccccc"]["Issues"])
        self.assertEqual(runs["20261112-100000-cccccc"]["KeyFormats"], "CC")
        self.assertEqual(runs["20261113-120000-dddddd"]["Complete"], "True", runs["20261113-120000-dddddd"]["Issues"])
        self.assertEqual({r["Key"] for r in res["legacy_keys"]}, {"CutAChoose_3_Chat", "PlayerData"})
        self.assertEqual(res["unknown_keys"], [])
        cc_rows = [r for r in res["rounds"] if r["KeyFormat"] == "CC"]
        self.assertEqual(len(cc_rows), 12)
        self.assertTrue(all(r["KeyScenarioCheck"] == "n/a" for r in cc_rows))

    def test_incomplete_run(self):
        items = {1: {}, 2: {}}
        add_run(items, "20261113-130000-eeeeee", "Info", "readable")
        del items[2]["CutAChoose_4_InfoAChat_20261113-130000-eeeeee_P2"]
        del items[2]["PlayerData_20261113-130000-eeeeee_P2"]
        res = self.export([("player-A.json", items[1]), ("player-B.json", items[2])])
        run = self.runs(res)["20261113-130000-eeeeee"]
        self.assertEqual(run["Complete"], "False")
        self.assertIn("P2 missing rounds [4]", run["Issues"])
        self.assertIn("P2 final record missing", run["Issues"])
        p2 = [p for p in res["players"] if p["PlayerRole"] == "2"][0]
        self.assertEqual((p2["RoundsPresent"], p2["RoundsMissing"], p2["FinalPresent"]), ("5", "4", "False"))

    def test_conflicting_duplicate_round(self):
        items = {1: {}, 2: {}}
        run = "20261113-140000-ffffff"
        add_run(items, run, "Chat", "readable")
        # a second record for P1 round 2 under another key (different condition in key and payload)
        items[1]["CutAChoose_2_Info_%s_P1" % run] = round_record(run, 1, 2, "Info", "player-A")
        res = self.export([("player-A.json", items[1]), ("player-B.json", items[2])])
        exp = self.runs(res)[run]
        self.assertEqual(exp["Complete"], "False")
        self.assertIn("P1 R2 duplicate records", exp["Issues"])
        dup_rows = [r for r in res["rounds"] if r["PlayerRole"] == "1" and r["ScenarioNum"] == "2"]
        self.assertEqual(len(dup_rows), 2)
        self.assertTrue(all(r["Duplicate"] == "True" for r in dup_rows))

    def test_key_condition_mismatch(self):
        items = {1: {}, 2: {}}
        run = "20261113-150000-121212"
        add_run(items, run, "Chat", "readable")
        k = "CutAChoose_3_Chat_%s_P2" % run
        items[2][k]["ScenarioType"] = "Info"   # payload disagrees with the key
        res = self.export([("player-A.json", items[1]), ("player-B.json", items[2])])
        exp = self.runs(res)[run]
        self.assertEqual(exp["Complete"], "False")
        self.assertIn("P2 R3 key condition Chat != record ScenarioType Info", exp["Issues"])
        self.assertEqual([r["KeyScenarioCheck"] for r in res["rounds"] if r["Key"] == k], ["MISMATCH"])

    def test_multiple_runs_never_merge_and_final_association(self):
        items = {1: {}, 2: {}}
        add_run(items, "20261113-160000-343434", "Info", "readable")
        add_run(items, "20261113-170000-565656", "Chat", "readable")
        res = self.export([("player-A.json", items[1]), ("player-B.json", items[2])])
        for run in ("20261113-160000-343434", "20261113-170000-565656"):
            rows = [r for r in res["rounds"] if r["RunId"] == run]
            self.assertEqual(len(rows), 12)
            self.assertTrue(all(run in r["Key"] for r in rows))
            players = [p for p in res["players"] if p["RunId"] == run]
            self.assertEqual(sorted(p["FinalKey"] for p in players),
                             ["PlayerData_%s_P1" % run, "PlayerData_%s_P2" % run])
            self.assertTrue(all(p["PaymentRoundResult"] != "" and p["WinningsCheck"] == "ok" for p in players))

    def test_unknown_keys_are_listed_not_dropped(self):
        items = {1: {}, 2: {}}
        add_run(items, "20261113-180000-787878", "Info", "readable")
        items[1]["SomethingElse_1"] = {"a": 1}
        items[1]["CutAChoose_1_Normal_20261113-180000-787878_P1x"] = {"a": 1}
        items[2]["PlayerData_20261113-180000-787878_P9"] = "not an object"
        res = self.export([("player-A.json", items[1]), ("player-B.json", items[2])])
        self.assertEqual({r["Key"] for r in res["unknown_keys"]},
                         {"SomethingElse_1", "CutAChoose_1_Normal_20261113-180000-787878_P1x",
                          "PlayerData_20261113-180000-787878_P9"})

    def test_key_patterns_are_disjoint(self):
        cases = {
            "CutAChoose_0_Normal_20261113-101500-a3f9c1_P1": "round",
            "CutAChoose_5_InfoAChat_L20261113-101500-a3f9c1_P2": "round",
            "PlayerData_20261113-101500-a3f9c1_P1": "final",
            "CC_20261113-101500-a3f9c1_P1_R0": "round",
            "CC_20261113-101500-a3f9c1_P1_final": "final",
            "CutAChoose_3_Chat": "legacy",
            "PlayerData": "legacy",
            "CutAChoose_3": "unknown",
        }
        for key, kind in cases.items():
            self.assertEqual(cc_export.classify(key)[0], kind, key)


if __name__ == "__main__":
    unittest.main(verbosity=2)
