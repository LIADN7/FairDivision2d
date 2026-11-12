# Anonymized export example (test data)

Output of `tools/research_export/cc_export.py` for two complete test experiments played on 2026-11-12 in **test mode** (Unity Services `development`, Photon `uxtest_2.42`) — not research data. The anonymous player IDs are replaced by `player-A (WebGL)` and `player-B (Editor)`; nicknames (`QA-Web`, `QA-Editor`) and URL IDs (`TEST_cs_w`, `TEST_cs_s`) are synthetic.

| File | Shows |
|---|---|
| `experiments.csv` | Two runs (`20261112-204719-a9f9be`, `20261112-210655-f04a40`), both players, `Complete = True`, same payment index on both sides, all six per-round cross-checks `ok` |
| `players.csv` | One row per run and role: six rounds each, final record present, payment round result and `Winnings` with the formula check |
| `rounds.csv` | 24 round records: condition per round (Normal, Normal, Chat, Chat, InfoAChat, InfoAChat), choices, `SumOfMyPart`, see-other counts, chat message counts, `isAgreed` |
| `example_records.json` | One stored round record (Chat round with three messages; the 474-cell map is shortened) and one stored final record |

In the second run the roles are swapped (the Editor created the room), so `player-B` is Player 1 there.

An incomplete experiment is reported as, for example:

```
20261112-210655-f04a40,True,Room_1,1 2,2,False,False,R0:ok R1:ok R2:ok R3:ok R4:ok,P2 missing rounds [5] | P2 final record missing
```

(produced by withholding two of player-A's run-2 records from the input). How to read the tables: [RESEARCH_DATA.md §6](../RESEARCH_DATA.md#6-export-and-analysis).
