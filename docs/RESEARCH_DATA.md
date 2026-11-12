# Research Data — Cloud Save Layout, Export and Validation

> Cloud Save & data integrity batch (`feat/cloud-save-data-integrity`, from `dev` `fd5917d`, 2026-11-12). Owner-approved: run-scoped keys, a shared experiment ID, confirmed saves and a two-step end screen. Research rules, valuations, conditions, payment and reward formula are unchanged. Evidence for every statement below: [PROJECT_CONTEXT §17 "Cloud Save & data integrity batch"](PROJECT_CONTEXT.md#17-current-status-and-handoff).

## 1. Two schemas in the same Cloud Save project

| | Schema 1 (legacy, all data collected before this batch) | Schema 2 (this batch onwards) |
|---|---|---|
| Round record key | `CutAChoose_<round>_<type>`, e.g. `CutAChoose_3_Chat` | `CC_<runId>_P<role>_R<round>`, e.g. `CC_20261112-204719-a9f9be_P2_R3` |
| Reward record key | `PlayerData` | `CC_<runId>_P<role>_final` |
| Value | a JSON **string** containing the record | a JSON **object** (same field names and values) |
| Link between the two players | nicknames only (`PlayerName` / `otherPlayerName`) | shared `RunId` + `PlayerRole` |
| Repeat play by the same anonymous player | overwrites the previous run's keys | new keys per run; nothing is overwritten |

Cloud Save returns object fields in alphabetical order (field order is not meaningful in JSON). WebGL timestamps have millisecond precision (e.g. `2026-11-12T22:55:02.269+02:00`), Editor ones 100 ns; the stored text is the client's own. Identify the schema by the key: anything starting with `CC_` is schema 2; `CutAChoose_*` and `PlayerData` are schema 1. Schema 2 records also carry `"SchemaVersion": 2`. The batch never writes, migrates or deletes schema 1 keys; analyse the two schemas separately. Within schema 1, a key holds only the **last** run of that anonymous player (earlier runs of the same browser were overwritten), and the two players of a pair can be matched only through nicknames, round and condition.

## 2. Identities

| Identity | Where it comes from | Field |
|---|---|---|
| Experiment (run) | Drawn once by the client that **creates** the Photon room and stored as the custom room property `ccRunId` at creation (`RoomOptions.CustomRoomProperties`, applied only when the room is created). The joining client receives the same property with the room. Format `yyyyMMdd-HHmmss-xxxxxx` (UTC creation time + 24 random bits, `System.Guid`; `UnityEngine.Random` is not used, so gameplay draws are unchanged). Cached on each client until the lobby starts a new experiment (room button) | `RunId` |
| Shared or local | `true` when the ID came from the room. `false` only if a room had no property (e.g. a room created by an older build): the client then uses a local ID prefixed with `L`, which the partner cannot share — analyse such runs by nickname as in schema 1 | `RunIdShared` |
| Pair / session | Photon room name (`Room_1` … `Room_3`) plus `RunId` | `RoomName` |
| Player 1 / Player 2 | `PhotonNetwork.IsMasterClient` at the start of each real round (1 = master = first chooser, receives the remainder) | `PlayerRole`, and `P<role>` in the key |
| Unity player | Anonymous Unity Authentication player ID (owner of the record; one per browser profile and origin, cached session token) | `UnityPlayerId` |
| Round | `ScenarioNum` 0–5 (shown to players as round 1–6) and `R<round>` in the key | `ScenarioNum`, `ScenarioType` |
| Survey participant | URL parameters `userID`, `surveyID` (WebGL), else `defaultUserID` / `defaultSurveyID` — unchanged | `userID` / `surveyID` (round), `UserID` / `SurveyID` (final) |

Two players who share one anonymous identity (two tabs of the same browser profile on the same site) still get different keys, because the role is part of the key.

## 3. Record layout

**Round record** (`CC_<runId>_P<role>_R<round>`, one per player and round, saved when the player presses Next). Added fields, all at the top: `SchemaVersion` (2), `RecordType` (`"round"`), `RunId`, `RunIdShared`, `PlayerRole`, `UnityPlayerId`, `RoomName`. All historical fields keep their names, values and meanings (PROJECT_CONTEXT §11): `userID`, `surveyID`, `ScenarioNum`, `ScenarioType`, `PlayerName`, `otherPlayerName`, `ChatHistory`, `isAgreed`, `PlayerCutValues`, `PlayerColorValues`, `OtherPlayerColorValues`, `AmountOfColor`, `PlayerStartTime`, `PlayerTimeCut`, `CountPlayerSeeOther`, `PlayerChoosenColors`, `SumOfMyPart`, `playerCutSquares`. Dates keep the original Newtonsoft text (client local time with offset). Valid zero/empty values: `CountPlayerSeeOther = 0`, empty `ChatHistory`, `isAgreed = false`, an empty region (`0` value, `0` cells).

**Final record** (`CC_<runId>_P<role>_final`, saved by the end-screen Save button): `SchemaVersion`, `RecordType` (`"final"`), `RunId`, `RunIdShared`, `PlayerRole`, `UnityPlayerId`, `RoomName`, the unchanged legacy reward fields `SurveyID`, `UserID`, `Name`, `Email`, `PhoneNumber`, `ID`, `WinRandIndex`, `Winnings`, and `RoundKeys` (the round keys this client saved in the run). `Email`, `PhoneNumber` and `ID` are empty with the current lobby and end screen (their inputs are inactive); `Name` is the lobby nickname. `Winnings = 30 + max(0, (x − 45)·1.5)` with x = `SumOfMyPart` of round `WinRandIndex` (unchanged).

## 4. Save lifecycle

1. Next in a real round → the round record is frozen (a JSON snapshot) and submitted to `ResearchSaveQueue`. Gameplay continues at once; the save runs in the background and survives the scene load.
2. Each save is confirmed only when the Cloud Save call returns successfully (the service acknowledged the key). A failed attempt is retried up to 3 times per request (1 s, 3 s). After that the record stays `Failed` in memory, with its payload.
3. End screen, **Step A — Save** (`שלח`): registers the final record once (same reward and payment round on every retry), then retries every failed round record, waits for pending ones, and saves the final record. Shows `שומרים את הנתונים...` while saving. Success only when all 7 records of the player are confirmed: `הנתונים נשמרו בהצלחה.`, Save hidden, return button shown. Failure: `השמירה נכשלה. בדקו את החיבור לאינטרנט ולחצו שוב על "שלח".`, Save enabled for a manual retry. Repeated presses during a save are ignored.
4. **Step B — return** (`חזרה לאתר הסקר`): a separate click, only after a confirmed save, once per visit; calls the unchanged `Config.RedirectToSurvey` (panel4all URL with `surveyID`, `userID`, `status=finish`, `winnings`); never writes to Cloud Save. Test mode shows it disabled with `מצב בדיקה: המעבר לסקר אינו זמין.` (the redirect is suppressed there anyway). No automatic navigation.

Retries write the same key with the same payload, so they cannot create duplicate records. Practice is still never saved.

## 5. Cloud Save limits (checked 2026-11-12)

Source: Unity Cloud Save docs (player data concept page; Cloud Save client API spec). Per player and access class (this game uses the default class): **2000 keys** and **5 MiB** total; keys must match `[A-Za-z0-9_-]{1,50}`; 600 requests per minute per player.

| Measured (test run, 2026-11-12) | Size |
|---|---|
| Key length | 31 (round), 34 (final); local fallback +1 |
| Round record | 10.8–11.7 KB (≈10 KB is `playerCutSquares`, 474 cells) |
| Final record | 0.5 KB |
| One run, one player (6 rounds + final) | ≈68 KB, 7 keys |

A participant normally plays once: ≈68 KB of 5 MiB (≈1.3 %) and 7 of 2000 keys. A full chat history (max 200 messages, ≈120 bytes each) adds up to ≈24 KB per round, ≈210 KB per run in the worst case. A **test identity** that plays many runs reaches the 5 MiB limit after roughly 25–75 runs; switch the Authentication profile (or clear the browser's site data) for long test campaigns. Schema 1 keys of the same identity count toward the same limits.

## 6. Export and analysis

**Fetching (privileged, not automated here).** Reading every player's data needs a Unity service account with the `cloud_save.data.read` (and, to list players, `cloud_save.entities.list`) scopes — role *Cloud Save Viewer*. Endpoints (Cloud Save Admin API):
- list players with data: `GET https://services.api.unity.com/cloud-save/v1/data/projects/<projectId>/environments/<environmentId>/players` (`start`, `limit` ≤ 100);
- one player's items: `GET .../players/<playerId>/items?prefix=CC_` (pages of 20 keys; pass the last key as `after`).

Create the service account in the Unity dashboard, keep its key in a password manager or environment variables, and never commit it. The historical exporter `UnityCloudDataScraper.cs` contains exposed credentials (PROJECT_CONTEXT §11) and must not be reused. Use the `production` environment ID for research data and `development` only for tests. This step was **not** validated in this batch (no service account was used).

**Joining (validated).** Save each player's merged `results` as `<playerId>.json`, then:

```bash
python tools/research_export/cc_export.py out_dir exports/*.json
```

Outputs (standard library only, no network, no credentials; personal fields `Name`, `Email`, `PhoneNumber`, `ID` only with `--include-pii`):
- `experiments.csv` — one row per run: roles present, distinct player IDs, `Complete`, same payment index on both sides, per-round cross-checks (same condition, identical cell counts, mirrored region values, choices cover regions 0–3), issues.
- `players.csv` — one row per run and role: rounds present/missing, final present, `WinRandIndex`, payment round result, `Winnings` and a formula check.
- `rounds.csv` — one row per round record (identity, condition, cut values, region values both ways, cells, choices, `SumOfMyPart` check, see-other count, number of chat messages, `isAgreed`).
- `legacy_keys.csv` — schema 1 keys found (not parsed).

How to: **identify one experiment** — one `RunId` row in `experiments.csv`; **find both players** — `players.csv` rows with that `RunId` (`PlayerRole` 1 and 2, `OwnerPlayerId`); **locate the six rounds** — `rounds.csv` rows `RunId`, `PlayerRole`, `ScenarioNum` 0–5; **chat / info data** — `ChatHistory` and `CountPlayerSeeOther` in the round JSON (counts in `rounds.csv`); **reward** — `players.csv` `WinRandIndex`, `Winnings`; **two experiments** — different `RunId` (the prefix is the UTC creation time); **incomplete experiment** — `Complete = False` with the reason in `Issues` (missing role, missing rounds, missing final record).

## 7. Test-mode read-back (validation helper)

Editor and Development builds only (compiled out of Release): `DatabaseManager.TestReadBackAsync(prefix)` reads **the signed-in player's own** data back from Cloud Save — every key with its modification time and the full values of keys with the given prefix. In WebGL call `unityInstance.SendMessage("DatabaseManager", "TestReadBackRecords", "CC_<runId>")`; it logs `READBACK-PART <id> <i>/<n> <text>` lines (WebGL truncates a log line at 8191 characters); joining the parts gives the read-back document, which `cc_export.py` accepts directly. Read-only; it logs no tokens (the anonymous player ID is not a credential).

## 8. Limitations

- Nothing is stored locally: closing or reloading the tab before a save reaches the service loses that record (pending or failed). The end screen reports it only while the page is open.
- A round that is never finished with Next (Home, disconnect) is not saved (unchanged).
- A client hidden in a background tab stops (WebGL), so its saves are delayed until it is shown again.
- The Save button can be pressed again after a failure without limit; each press runs at most 3 attempts per unconfirmed record.
- Chat messages that arrive after a player pressed Next are not in that player's record (owner decision: not a priority).
- `RunIdShared = false` runs (room without the property) cannot be linked by ID.
- The survey provider is not integrated; the return button uses the existing panel4all redirect in Release (not exercised; suppressed in test mode).
