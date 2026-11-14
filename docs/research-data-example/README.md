# Anonymized export example (test data)

Output of `tools/research_export/cc_export.py` for test experiments played in **test mode** (Unity Services `development`, Photon `uxtest_2.42`) — not research data. Anonymous player IDs are replaced by `player-A (WebGL)`, `player-B (Editor)` (2026-11-12) and `player-C (Chrome)`, `player-D (Chrome)` (2026-11-13); nicknames (`QA-Web`, `QA-Editor`, `QA-KeysA`, `QA-KeysB`) and URL IDs (`TEST_…`) are synthetic.

The example mixes both schema 2 key styles to show that the exporter reads them side by side:

| Run | Key style | Content |
|---|---|---|
| `20261113-093438-da6d43` | readable: `CutAChoose_<round>_<ScenarioType>_<runId>_P<role>`, `PlayerData_<runId>_P<role>` | Complete two-client run (player-C = Player 1, player-D = Player 2): Normal, Normal, **Chat**, **Chat**, InfoAChat, InfoAChat; payment round 4 (index 3); `KeyScenarioCheck = ok` for every round |
| `20261112-204719-a9f9be`, `20261112-210655-f04a40` | earlier test style `CC_<runId>_P<role>_R<round>` / `_final` | Two complete runs (roles swapped in the second) |
| `20261112-221552-99791d`, `20261112-222139-5bc301` | `CC_` | Partial test runs, reported `Complete = False` with the missing rounds and final records in `Issues` |

| File | Shows |
|---|---|
| `experiments.csv` | One row per run: roles, distinct players, `Complete`, same payment index, per-round cross-checks, issues, `KeyFormats` |
| `players.csv` | One row per run and role: rounds present/missing, final record and its key, payment round result, `Winnings` with the formula check |
| `rounds.csv` | One row per round record: condition, choices, `SumOfMyPart`, see-other count (round 5 of the readable run: 2), chat message count, the key, its style and `KeyScenarioCheck` |
| `example_records.json` | One stored readable round record (`CutAChoose_2_Chat_…_P1`; the 474-cell map is shortened) and its final record (`PlayerData_…_P1`) |

`player-C` and `player-D` each played once with their own browser profile, so each has exactly seven keys. How to read the tables: [RESEARCH_DATA.md §6](../RESEARCH_DATA.md#6-export-and-analysis).
