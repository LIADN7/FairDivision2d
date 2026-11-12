#!/usr/bin/env python3
"""Join Fair Division 2D Cloud Save records (schema 2) into per-experiment tables.

Offline helper: it reads JSON files that were already exported from Unity Cloud Save and writes CSV files. It never
connects to Unity services and contains no credentials. Standard library only (Python 3.8+).

Accepted input files (one player per file, any mix):
  1. Test-mode read-back document written by DatabaseManager.TestReadBackAsync:
       {"playerId": "...", "allKeys": [...], "items": {"<key>": <value>, ...}}
  2. A Cloud Save REST "get items" response for one player, saved as <playerId>.json:
       {"results": [{"key": "...", "value": <value>, ...}, ...], "links": {...}}
     (Admin API, service account with cloud_save.data.read: GET https://services.api.unity.com/cloud-save/v1/data/
      projects/<projectId>/environments/<environmentId>/players/<playerId>/items?prefix=CC_ ; pages of 20 keys,
      pass the last key as ?after=<key> and merge the "results" of all pages first. See docs/RESEARCH_DATA.md.)
  3. A plain map {"<key>": <value>, ...} saved as <playerId>.json.

Schema 2 keys (written since the Cloud Save & data integrity batch):
  CC_<runId>_P<role>_R<round>   round record (round 0-5), role 1 = Player 1 (master), 2 = Player 2
  CC_<runId>_P<role>_final      final reward record
Schema 1 (legacy) keys CutAChoose_<round>_<type> and PlayerData carry no run ID; they are only counted here.

Usage:
  python cc_export.py OUT_DIR FILE [FILE ...] [--include-pii]

Outputs in OUT_DIR: experiments.csv (one row per run), players.csv (one row per run and role), rounds.csv (one row per
round record), legacy_keys.csv. Personal fields of the final record (Name, Email, PhoneNumber, ID) are left out unless
--include-pii is given. Values stored as JSON strings (legacy style) are decoded automatically.
"""
import csv
import json
import os
import re
import sys

ROUND_KEY = re.compile(r"^CC_(?P<run>[A-Za-z0-9-]+)_P(?P<role>\d)_R(?P<round>\d+)$")
FINAL_KEY = re.compile(r"^CC_(?P<run>[A-Za-z0-9-]+)_P(?P<role>\d)_final$")
LEGACY_KEY = re.compile(r"^(CutAChoose_\d+_[A-Za-z]+|PlayerData)$")
REGIONS = ["RedX", "GreenO", "RedO", "GreenX"]  # region indices 0-3 (Red, Green, Yellow, Blue on screen)
PII_FIELDS = ["Name", "Email", "PhoneNumber", "ID"]
EXPECTED_ROUNDS = 6


def decode(value):
    """Schema 2 stores JSON objects; legacy records are JSON strings. Return a dict when possible."""
    if isinstance(value, str):
        try:
            return json.loads(value)
        except ValueError:
            return value
    return value


def load_player_file(path):
    with open(path, encoding="utf-8") as f:
        doc = json.load(f)
    player_id = os.path.splitext(os.path.basename(path))[0]
    if isinstance(doc, dict) and "items" in doc and isinstance(doc["items"], dict):
        return doc.get("playerId") or player_id, {k: decode(v) for k, v in doc["items"].items()}, \
            [k["key"] for k in doc.get("allKeys", [])] or list(doc["items"].keys())
    if isinstance(doc, dict) and "results" in doc:
        items = {r["key"]: decode(r.get("value")) for r in doc["results"]}
        return player_id, items, list(items.keys())
    if isinstance(doc, dict):
        return player_id, {k: decode(v) for k, v in doc.items()}, list(doc.keys())
    raise ValueError(path + ": unsupported JSON layout")


def winnings_formula(x):
    return 30 + max(0.0, (x - 45) * 1.5)


def main(argv):
    include_pii = "--include-pii" in argv
    args = [a for a in argv if a != "--include-pii"]
    if len(args) < 2:
        print(__doc__)
        return 2
    out_dir, files = args[0], args[1:]
    os.makedirs(out_dir, exist_ok=True)

    rounds = {}   # (run, role, round) -> (owner playerId, record)
    finals = {}   # (run, role) -> (owner playerId, record)
    legacy = []   # (owner playerId, key)
    for path in files:
        owner, items, all_keys = load_player_file(path)
        for key, value in items.items():
            m = ROUND_KEY.match(key)
            if m and isinstance(value, dict):
                rounds[(m["run"], int(m["role"]), int(m["round"]))] = (owner, value)
                continue
            m = FINAL_KEY.match(key)
            if m and isinstance(value, dict):
                finals[(m["run"], int(m["role"]))] = (owner, value)
        legacy += [(owner, k) for k in all_keys if LEGACY_KEY.match(k)]

    runs = sorted({k[0] for k in rounds} | {k[0] for k in finals})
    round_cols = ["RunId", "PlayerRole", "ScenarioNum", "ScenarioType", "OwnerPlayerId", "UnityPlayerId", "RunIdShared",
                  "RoomName", "userID", "surveyID", "PlayerStartTime", "PlayerTimeCut", "CutRed", "CutGreen"] \
        + ["Own_" + r for r in REGIONS] + ["Other_" + r for r in REGIONS] + ["Cells_" + r for r in REGIONS] \
        + ["Chosen1", "Chosen2", "SumOfMyPart", "SumCheck", "CountPlayerSeeOther", "ChatMessages", "isAgreed",
           "PlayerName", "otherPlayerName"]
    player_cols = ["RunId", "PlayerRole", "OwnerPlayerId", "RoundsPresent", "RoundsMissing", "FinalPresent",
                   "WinRandIndex", "PaymentRoundResult", "Winnings", "WinningsCheck", "UserID", "SurveyID",
                   "RoundKeysListed"] + (PII_FIELDS if include_pii else [])
    exp_cols = ["RunId", "RunIdShared", "RoomName", "Roles", "DistinctPlayerIds", "Complete", "SameWinRandIndex",
                "CrossChecks", "Issues"]

    with open(os.path.join(out_dir, "rounds.csv"), "w", newline="", encoding="utf-8") as fr, \
            open(os.path.join(out_dir, "players.csv"), "w", newline="", encoding="utf-8") as fp, \
            open(os.path.join(out_dir, "experiments.csv"), "w", newline="", encoding="utf-8") as fe:
        wr, wp, we = csv.writer(fr), csv.writer(fp), csv.writer(fe)
        wr.writerow(round_cols)
        wp.writerow(player_cols)
        we.writerow(exp_cols)
        for run in runs:
            issues, cross = [], []
            roles = sorted({k[1] for k in rounds if k[0] == run} | {k[1] for k in finals if k[0] == run})
            owners = set()
            win_index = {}
            for role in roles:
                present = sorted(r for (ru, ro, r) in rounds if ru == run and ro == role)
                missing = [r for r in range(EXPECTED_ROUNDS) if r not in present]
                for r in present:
                    owner, rec = rounds[(run, role, r)]
                    owners.add(rec.get("UnityPlayerId") or owner)
                    own, other, cells = rec.get("PlayerColorValues", {}), rec.get("OtherPlayerColorValues", {}), \
                        rec.get("AmountOfColor", {})
                    chosen = rec.get("PlayerChoosenColors", [-1, -1])
                    total = sum(own.get(REGIONS[i], 0) for i in chosen if 0 <= i < 4)
                    sum_ok = abs(total - rec.get("SumOfMyPart", 0)) < 1e-3
                    if not sum_ok:
                        issues.append("P%d R%d SumOfMyPart != own values of chosen regions" % (role, r))
                    if rec.get("ScenarioNum") != r:
                        issues.append("P%d R%d ScenarioNum %s" % (role, r, rec.get("ScenarioNum")))
                    if rec.get("RunId") != run or rec.get("PlayerRole") != role:
                        issues.append("P%d R%d key/record identity mismatch" % (role, r))
                    cut = rec.get("PlayerCutValues", {})
                    wr.writerow([run, role, r, rec.get("ScenarioType"), owner, rec.get("UnityPlayerId"),
                                 rec.get("RunIdShared"), rec.get("RoomName"), rec.get("userID"), rec.get("surveyID"),
                                 rec.get("PlayerStartTime"), rec.get("PlayerTimeCut"), cut.get("Red"), cut.get("Green")]
                                + [own.get(x) for x in REGIONS] + [other.get(x) for x in REGIONS]
                                + [cells.get(x) for x in REGIONS]
                                + [chosen[0] if len(chosen) > 0 else None, chosen[1] if len(chosen) > 1 else None,
                                   rec.get("SumOfMyPart"), "ok" if sum_ok else "MISMATCH",
                                   rec.get("CountPlayerSeeOther"), len(rec.get("ChatHistory") or []),
                                   rec.get("isAgreed"), rec.get("PlayerName"), rec.get("otherPlayerName")])
                if missing:
                    issues.append("P%d missing rounds %s" % (role, missing))
                fin = finals.get((run, role))
                win_res, win_ok = None, None
                if fin:
                    owner, rec = fin
                    owners.add(rec.get("UnityPlayerId") or owner)
                    idx = rec.get("WinRandIndex")
                    win_index[role] = idx
                    if (run, role, idx) in rounds:
                        win_res = rounds[(run, role, idx)][1].get("SumOfMyPart")
                        win_ok = abs(winnings_formula(win_res) - rec.get("Winnings", -1)) < 1e-2
                        if not win_ok:
                            issues.append("P%d Winnings != 30 + max(0, (x - 45) * 1.5)" % role)
                    listed = rec.get("RoundKeys") or []
                    unlisted = [k for k in listed if ROUND_KEY.match(k) and (run, role, int(ROUND_KEY.match(k)["round"])) not in rounds]
                    if unlisted:
                        issues.append("P%d final lists round keys not found: %s" % (role, unlisted))
                    wp.writerow([run, role, owner, len(present), " ".join(map(str, missing)), True, idx, win_res,
                                 rec.get("Winnings"), {None: "n/a", True: "ok", False: "MISMATCH"}[win_ok],
                                 rec.get("UserID"), rec.get("SurveyID"), len(listed)]
                                + ([rec.get(x) for x in PII_FIELDS] if include_pii else []))
                else:
                    issues.append("P%d final record missing" % role)
                    wp.writerow([run, role, "", len(present), " ".join(map(str, missing)), False, "", "", "", "n/a",
                                 "", "", ""] + (["" for _ in PII_FIELDS] if include_pii else []))
            if roles != [1, 2]:
                issues.append("roles present %s (expected [1, 2])" % roles)
            # Cross-player checks per round: shared condition, identical cell counts, mirrored region values.
            for r in range(EXPECTED_ROUNDS):
                a, b = rounds.get((run, 1, r)), rounds.get((run, 2, r))
                if not (a and b):
                    continue
                a, b = a[1], b[1]
                ok = a.get("ScenarioType") == b.get("ScenarioType") and a.get("AmountOfColor") == b.get("AmountOfColor") \
                    and all(abs(a["OtherPlayerColorValues"][x] - b["PlayerColorValues"][x]) < 1e-3 for x in REGIONS) \
                    and all(abs(b["OtherPlayerColorValues"][x] - a["PlayerColorValues"][x]) < 1e-3 for x in REGIONS)
                got = sorted([i for i in a.get("PlayerChoosenColors", []) + b.get("PlayerChoosenColors", []) if i >= 0])
                ok = ok and got == [0, 1, 2, 3]
                cross.append("R%d:%s" % (r, "ok" if ok else "MISMATCH"))
                if not ok:
                    issues.append("R%d cross-player mismatch" % r)
            first = next((rounds[k][1] for k in sorted(rounds) if k[0] == run), None) or \
                next((finals[k][1] for k in sorted(finals) if k[0] == run), {})
            same_idx = len(set(win_index.values())) == 1 and len(win_index) == 2
            complete = not issues
            we.writerow([run, first.get("RunIdShared"), first.get("RoomName"), " ".join(map(str, roles)), len(owners),
                         complete, same_idx, " ".join(cross), " | ".join(issues)])

    with open(os.path.join(out_dir, "legacy_keys.csv"), "w", newline="", encoding="utf-8") as fl:
        wl = csv.writer(fl)
        wl.writerow(["OwnerPlayerId", "Key", "Schema"])
        for owner, key in sorted(set(legacy)):
            wl.writerow([owner, key, 1])
    print("runs: %d, round records: %d, final records: %d, legacy keys: %d -> %s"
          % (len(runs), len(rounds), len(finals), len(set(legacy)), out_dir))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
