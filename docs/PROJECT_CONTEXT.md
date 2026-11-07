# Fair Division 2D — Project Context

> Overview for developers and AI agents joining the project. The full implementation reference for the core algorithm — cells, cuts, regions, selection, synchronization, state machines, dependency map, coupling risks and research invariants — is **[CUT_AND_CHOOSE_REFERENCE.md](CUT_AND_CHOOSE_REFERENCE.md)**. This file does not repeat it.
>
> **Last verified:** 2026-11-07 on `feat/ux-batch-6` (from `dev` `9103593`, the Batch 5 merge): static inspection plus Editor checks through Unity Pipeline and two-client tests in test mode (earlier baselines `8b29582`, `0f3f99d`, `1f7b01d`). **New agent? Start with [§17 Current Status and Handoff](#17-current-status-and-handoff).**
>
> **Evidence tags used in both documents**
> - **[Verified]** — confirmed in C# source or serialized scene/prefab data.
> - **[Inferred]** — follows from reading the code, but was not reproduced at runtime.
> - **[README]** — claim from `README.md` that the code does not (fully) confirm.
> - **[Unknown]** — could not be determined from the repository; needs the research owners.

---

## 1. Project Overview

Fair Division 2D is a two-player, browser-based research game in which participants divide a 2D map (nine Ulster counties) using a fair-division protocol. It is part of a Master's thesis in Computer Science at the Open University (see `README.md` for supervisors and the research website).

| Item | Value |
|---|---|
| Engine | Unity **6000.2.10f1** ([ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt)) [Verified] |
| Multiplayer | Photon PUN **2.42** (`PunVersion` in `Assets/Photon/PhotonUnityNetworking/Code/PhotonNetwork.cs`) [Verified] |
| Persistence | Unity Gaming Services Cloud Save 3.2.2 + anonymous Authentication [Verified] |
| Target platform | WebGL [Inferred — `Assets/Plugins/MasterURL.jslib`, `RuntimePlatform.WebGLPlayer` branch in `Player.SetDataFromURL`, Web module installed; the active build target is not stored in the repo] |
| UI languages | English + Hebrew (RTL via `Assets/RTLTMPro`) [Verified] |
| Main algorithm in use | Symmetric Cut and Choose, 2 players [Verified] |
| Current branch focus | `AITutorial` — adds a practice round against a computer opponent [Verified] |

**Where to start reading:** [Player.cs](../Assets/Script/Player.cs) (rules, networking and round flow), [LineDrawingManager.cs](../Assets/Script/RefactoredUI/LineDrawingManager.cs) and [SquareLineCalculator.cs](../Assets/Script/RefactoredUI/SquareLineCalculator.cs) (cutting), [Config.cs](../Assets/Script/Config.cs) (conditions).

---

## 2. Research Purpose

From `README.md` [README]: compare structured fair-division algorithms with negotiation-based division, and measure the effect of (a) private vs. visible valuations and (b) communication (chat).

What the code implements [Verified]: four conditions, defined in [Config.cs](../Assets/Script/Config.cs) `createConfig` / `getScenarioTypeName`:

| Scenario # | Name (stored in data) | Chat | See other player's valuations |
|---|---|---|---|
| 1 | `Normal` | No | No |
| 2 | `Info` | No | Yes |
| 3 | `Chat` | Yes | No |
| 4 | `InfoAChat` | Yes | Yes |

The README also mentions envy, social welfare and geometric analysis. The game stores only the raw inputs (§11); no fairness metric is computed in-game [Verified].

---

## 3. Core Gameplay Overview

1. **Lobby** (`MainScene`): a participant types a name (optional email) and clicks a room button; two participants in the same room are paired.
2. **Practice** (`CutSceneAITutorial`): one guided round against a local computer opponent. Not paid, not saved.
3. **Six real rounds** (`CutScene`, reloaded each round): the two humans play against each other under the scheduled conditions (§6).
4. **End** (`EndGameFlow`): one real round is drawn for payment; the participant is redirected to an external survey panel.

Each round: **Cut** (each player draws one straight line; cells fall on a Red or Green side) → **Combine** (the two cuts give four regions) → **Choose** (Player 1 takes one region, Player 2 two, Player 1 gets the remainder) → **Result** (share of the player's *own* total valuation).

---

## 4. Symmetric Cut and Choose — Summary

| Topic | Key facts | Detail |
|---|---|---|
| Cells and valuations | 474 cells (`PointOfState`), 9 value-uniform counties, values 2/4/8; totals P1 1976, P2 2152; same map every round | [Reference §1](CUT_AND_CHOOSE_REFERENCE.md#1-map-cells-and-valuation-data) |
| Cutting | One straight segment per player, treated as an infinite line; cell centre side via 2D cross product; no validation | [§2](CUT_AND_CHOOSE_REFERENCE.md#2-line-drawing-validation-and-cell-classification) |
| Regions | Red (both Red), Green (both Green), Yellow (P1 Red / P2 Green), Blue (P1 Green / P2 Red); data keys `RedX, GreenO, RedO, GreenX`; may be empty | [§3](CUT_AND_CHOOSE_REFERENCE.md#3-the-four-regions) |
| Selection | Replicated `chooseNum` gates P1 → P2 → P2; remainder = first still-interactable button | [§4](CUT_AND_CHOOSE_REFERENCE.md#4-the-1-2-1-selection-sequence) |
| Scoring | `100 · Σ own values / own total`, summed over received regions, per client | [§5](CUT_AND_CHOOSE_REFERENCE.md#5-allocation-percentages) |
| Roles and authority | Player 1 = Photon master client, fixed for the session; master only drives init, scene loads and the payment draw | [§6](CUT_AND_CHOOSE_REFERENCE.md#6-roles-host-authority-and-state-ownership) |
| Synchronization | RPC-only (`InitSquares`, `Cut`, `Choose`, `GetValues`, …); each client computes regions itself | [§7](CUT_AND_CHOOSE_REFERENCE.md#7-photon-communication-and-synchronization) |
| State machines | Session and per-round states with guards | [§8](CUT_AND_CHOOSE_REFERENCE.md#8-state-machines-lobby-to-results) |
| Worked example | 8-cell example from cut to payment | [§11](CUT_AND_CHOOSE_REFERENCE.md#11-worked-numerical-example) |
| Edge cases | Disconnects, Home, Next race, empty cuts | [§15](CUT_AND_CHOOSE_REFERENCE.md#15-edge-cases-and-failure-scenarios) |

**Owner-described rules vs. implementation:** all seven stated rules (two players; independent halving; four regions; P1 one, P2 two, P1 remainder; own-valuation percentages) are matched by the code. No direct contradiction was found. Behaviors the rules do not specify — and that need owner confirmation — are listed in §15.

---

## 5. Numerical Gameplay Example

See [Reference §11](CUT_AND_CHOOSE_REFERENCE.md#11-worked-numerical-example). In short: with both players cutting 50/50 on an 8-cell map, Player 1 picks a 30% region, Player 2 picks 30% + 20%, Player 1 receives a 30% remainder → results 60% and 50%; payment if that round is drawn: 52.5 and 37.5.

---

## 6. Game Flow and Experiment Rounds

### 6.1 Scene flow

```
MainScene ──room button──► CutSceneAITutorial (practice, vs PC)
                                  │ both humans click Next
                                  ▼
                      CutScene ×6 (rounds 0–5, reloaded each round)
                                  │ after round 6
                                  ▼
                          EndGameFlow ──Finish──► external survey redirect
```

Active lobby buttons Room 1–3 all start with practice on this branch (`Launcher.sceneToLoad = "CutSceneAITutorial"`, [Launcher.cs:28](../Assets/Script/Launcher.cs#L28)). Build settings also include `CutAI`, `DemoScene`, `RRScene`, `RRTestScene`; their lobby buttons are inactive [Verified].

### 6.2 Practice round

Each human plays against a local computer opponent (random choice among four predefined cuts, uniform-random picks), guided by `TutorialFplanManager` panels. Same map and valuations as `CutScene`; the result is not added to the payment list and no record is saved. Leaving practice requires both humans in the room to click Next. Mechanics: [Reference §8.3](CUT_AND_CHOOSE_REFERENCE.md#83-practice-scene-differences).

### 6.3 Round schedule

Implemented in `Player.MasterNextGame` ([Player.cs:854](../Assets/Script/Player.cs#L854)) [Verified]; mechanism in [Reference §9](CUT_AND_CHOOSE_REFERENCE.md#9-round-progression-and-experiment-conditions).

| Round shown to the player | `roundNumber` (stored `ScenarioNum`) | Condition |
|---|---|---|
| 1, 2 | 0, 1 | `Normal` |
| 3, 4 | 2, 3 | `Info` **or** `Chat` — drawn once by the master, identical for both players and both rounds |
| 5, 6 | 4, 5 | `InfoAChat` |

Matches the README. Map, valuations and player roles are identical in all six rounds [Verified]. The practice round also displays "round number 1".

### 6.4 Experimental rules vs. UI presentation

| Aspect | Experimental rule (code behavior) | UI presentation only |
|---|---|---|
| Own valuations | Always visible: cell shading and county percentages (`SumSquares.SetMyTitle`) | Shading tint ∝ value / total × 500; cells with value < 4 get no tint |
| Info condition | "See other player" toggle shows the opponent's valuation, **only before the player's own cut** (`Manager.changeSeeOtherPlayerBT` / `initialSeeOtherPlayerBT`); Cut button hidden while viewing | Button turns red; 5-decimal readouts |
| Chat condition | `ChatLayer` active; messages logged in **both** players' records (max 200; last 10 shown); "You reached an agreement in the chat?" toggle saved as `isAgreed` | Chat formatting |
| Help page | `timeForHelpPage = -1` in every round → no countdown | Help content |
| Instructions | Bilingual notes, including "divide into 2 equal parts → at least 50%", are part of the participant-facing protocol | Layout, colors |

### 6.5 Reward calculation [Verified]

After round 6 the master draws a round index uniformly from 0–5 and shares it; each client computes **winnings = 30 + max(0, (x − 45) × 1.5)** ₪ with x = its own result in that round (`PlayerData.SetNormalizeWinning`, [PlayerData.cs:138](../Assets/Script/PlayerData.cs#L138)). `EndGameFlow` shows the amount; Finish saves `PlayerData` and redirects to the panel4all survey with `surveyID`, `userID`, `status=finish`, `winnings` (`Config.RedirectToSurvey`). Data path: [Reference §10](CUT_AND_CHOOSE_REFERENCE.md#10-persistence-and-result-calculation).

---

## 7. Technologies and Dependencies

| Dependency | Version / location | Role |
|---|---|---|
| Unity Editor | 6000.2.10f1 | Engine |
| Photon PUN 2 (+ Realtime, Chat libs) | 2.42, `Assets/Photon` | Rooms, RPCs, scene sync. Settings: `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset` (contains the App ID — do not copy) |
| UGS Cloud Save / Authentication | 3.2.2 / 3.4.1 (lock file) | Per-player JSON records, anonymous sign-in |
| Newtonsoft JSON | 3.2.1 | Record serialization |
| uGUI + TextMesh Pro + RTLTMPro | `Assets/RTLTMPro` | UI, Hebrew text |
| LeanTween, FullSerializer, RestClient | `Assets/...` | Among project scripts only `RRScripts/SquareRR.cs` references them — not used by Cut and Choose [Verified] |
| External Dependency Manager + `StreamingAssets/google-services-desktop.json` | `Assets/ExternalDependencyManager` | Firebase-style config present; no script references Firebase [Verified] |
| `com.unity.test-framework` | 1.6.0 | Installed, unused by project code |
| `MasterURL.jslib` | `Assets/Plugins` | Reads URL query parameters in WebGL |
| CSharpier | 1.3.0 (`dotnet-tools.json`) | C# formatter (local dotnet tool) |

---

## 8. Project Structure and Important Components

### 8.1 Scenes (`Assets/Scenes`, all in build settings)

| Scene | Purpose | Reachable from active UI |
|---|---|---|
| `MainScene` | Lobby, Photon connect, room buttons (`Launcher`) | Entry |
| `CutSceneAITutorial` | Practice vs PC (`PlayerAITutorial`, `TutorialFplanManager`, `PCPlayerAITutorial`) | Yes |
| `CutScene` | Real rounds (`Player`, `Manager`, `LineDrawingManager`, 474 `PointOfState`, 9 `SumSquares`) | Yes |
| `EndGameFlow` | Payment display, survey redirect (`EndGamePlayerDataManager`) | Yes |
| `CutAI` | Older single-player vs AI (`PlayerVsAI`, `PointOfStateAI`) | No |
| `RRScene`, `RRTestScene`, `DemoScene` | Round Robin prototype (`Assets/Script/RRScripts`) and demo | No |
| `Assets/_Recovery/0.unity` | Editor crash-recovery artifact | — |

### 8.2 Scripts (`Assets/Script`)

| Area | Files | Responsibility |
|---|---|---|
| C&C core | `Player.cs` | Rules, scoring, RPCs, chat, data record, URL params, round schedule, payment draw, scene changes |
| | `Manager.cs` | Scene UI state: views, notes, Cut/Next buttons, see-other toggle, region % cache (`sumRGYB`), end-of-round panel |
| | `PointOfState.cs`, `CutAndChooseClasses/SumSquares.cs` | Cell values/status/shading; county totals and titles |
| | `RefactoredUI/LineDrawingManager.cs`, `RefactoredUI/SquareLineCalculator.cs` | Line input, side classification |
| Session / config | `Config.cs`, `Launcher.cs`, `LoadScene.cs`, `PlayerData.cs`, `CutAndChooseClasses/Utils/RandArrays.cs` | Conditions and round counter, lobby, scene changes, participant data and winnings, per-round results |
| Persistence | `DatabaseManager.cs`, `CutAndChooseClasses/CutAndChoosedatabase.cs`, `ChatMessage.cs` | Cloud Save wrapper; per-round record schema |
| End | `EndGamePlayerDataManager.cs` | Winnings text, save, redirect |
| Practice forks | `AITutorial/*` (`PlayerAITutorial`, `PointOfStateAITutorial`, `LineDrawingManagerAITutoria`, `SquareLineCalculatorAITutoria`, `SumSquaresAITutorial`, `HashValuesAITutorial`, `ShowAndHideOnClickAITutoria`, `HelpPageCountdownAITutoria`), `TutorialFplanManager`, `PCPlayerAITutorial` | Renamed copies of the core classes plus the tutorial and PC opponent |
| UI helpers | `HelpPageCountdown.cs`, `ShowAndHideOnClick.cs`, `Hover.cs` | Help panel, show/hide |
| Tools | `Editor/LiveGameDataWindow.cs` (*Tools → Live Game Data Monitor*), `CutAndChooseClasses/DataScripts/UnityCloudDataScraper.cs` | Play-mode monitor; offline Cloud Save export |
| Legacy / unused in C&C | `GameManager.cs`, `ChooseButton.cs`, `MapCut.cs`, `StateObj.cs`, `CutAndChooseClasses/ColorButtons.cs`, `HashValues.cs`, `PlayerVsAI.cs`, `PointOfStateAI.cs`, `DemoScripts/*`, `RRScripts/*` | Earlier iterations or other algorithms |

### 8.3 Prefabs (`Assets/Objects`)

`Config.prefab` (all game scenes; default `timeForHelpPage` 10, overwritten by `createConfig`), `PlayerData.prefab`, `DatabaseManager.prefab` (`MainScene`, `EndGameFlow`; also auto-created by `Manager.Start`), `CutUtilsObj/RandArrays.prefab` (`CutScene`, `CutSceneAITutorial`, `EndGameFlow`), `EndGamePlayerDataManager.prefab`, `RRPrefab/*`. Persistent singletons use `DontDestroyOnLoad`.

---

## 9. Networking and State Synchronization

- **Connection** [Verified]: `PhotonNetwork.ConnectUsingSettings()` → `JoinLobby()`; `AutomaticallySyncScene = true`; fixed room names `Room_1` … `Room_4` with occupancy shown from `OnRoomListUpdate`; a failed join returns to the menu with a status message (`OnJoinRoomFailed`).
- **Effective Photon version** [Verified at runtime]: `Launcher` sets `GameVersion = "0.0.1"`, but `ConnectUsingSettings()` overwrites it with the settings asset's empty `AppVersion`, so **Release/production clients connect as `_2.42`**. Editor and Development builds connect as **`uxtest_2.42`** (§13.2). Do not "fix" the `"0.0.1"` line: changing the production version would split new builds from deployed ones.
- **Model**: RPC-only, peer-symmetric, no room properties; master = Player 1; no reconnection or master-switch handling. Since Batch 6 (`feat/ux-batch-6`) a display-only notice (`GameSessionDialogs`) reports a confirmed departure or a lost connection (§17); nothing else changes.
- RPC catalogue, message sequence and state ownership: [Reference §6–§7](CUT_AND_CHOOSE_REFERENCE.md#6-roles-host-authority-and-state-ownership).

---

## 10. Player Valuations and Result Calculations

Two fixed additive valuations, serialized per cell (`myVal1`, `myVal2`) in `CutScene` and duplicated in `CutSceneAITutorial`; totals **P1 1976, P2 2152**. Per-county table and formulas: [Reference §1](CUT_AND_CHOOSE_REFERENCE.md#1-map-cells-and-valuation-data) and [§5](CUT_AND_CHOOSE_REFERENCE.md#5-allocation-percentages). Display uses raw floats (county titles 2 decimals, opponent readouts 5); stored values are unrounded.

---

## 11. Data Collection and Storage

**Per-round record** — `CutAndChoosePlayerDatabase` ([CutAndChoosedatabase.cs](../Assets/Script/CutAndChooseClasses/CutAndChoosedatabase.cs)), one per player per round, serialized with Newtonsoft and saved by `DatabaseManager.SaveData` → `CloudSaveService.Instance.Data.ForceSaveAsync` under key **`CutAChoose_{ScenarioNum}_{ScenarioType}`** (e.g. `CutAChoose_3_Chat`) in the anonymous player's Cloud Save data. When each field is written: [Reference §10](CUT_AND_CHOOSE_REFERENCE.md#10-persistence-and-result-calculation).

| Field | Content |
|---|---|
| `userID`, `surveyID` | From the page URL query (WebGL), else `defaultUserID` / `defaultSurveyID` |
| `ScenarioNum`, `ScenarioType` | Round index 0–5; `Normal` / `Info` / `Chat` / `InfoAChat` |
| `PlayerName`, `otherPlayerName` | Lobby nicknames (the only link between the two records of a pair) |
| `PlayerStartTime`, `PlayerTimeCut` | `DateTime.Now` (client local time, no time zone) at scene start and at Cut |
| `PlayerCutValues {Red, Green}` | Own % of own cut's sides |
| `playerCutSquares` | `{cellKey: {status: countyName}}` — own cut per cell; keys are not stable IDs (Reference §13.1) |
| `PlayerColorValues`, `OtherPlayerColorValues` | Region %s in own / opponent's valuation (`RedX, GreenO, RedO, GreenX` = regions 1–4) |
| `AmountOfColor` | Cell count per region |
| `CountPlayerSeeOther` | Clicks on the see-other toggle (on **and** off) |
| `PlayerChoosenColors[2]` | Region indices received: P1 `[first pick, remainder]`, P2 `[first, second]` |
| `SumOfMyPart` | Final own % |
| `ChatHistory` | `{Message, PlayerName, Date}` list (max 200) |
| `isAgreed` | Agreement toggle |

Saved when the player clicks Next. Not saved: practice, rounds left via Home or disconnect, failed cloud initialization.

**Participant record** — `PlayerData` key `PlayerData`: SurveyID, UserID, Name, Email, PhoneNumber, ID, WinRandIndex, Winnings; saved on the EndGameFlow Finish button. Contains **personal data** (name, email).

**Tooling and leftovers**: `UnityCloudDataScraper` (REST export of Cloud Save data); `LiveGameDataWindow` (Editor monitor); `Assets/DataFiles/*` and root `CutAndChhosedatabase.dat` are old local test exports (content not reviewed).

> **Security — separate remediation required:** `UnityCloudDataScraper.cs` contains a hard-coded project ID and **commented-out service-account credentials committed to Git**. Treat them as exposed: rotate them in the Unity dashboard and remove them from the repository (and its history if required) as a separate, approved task. Never copy, print or test them.

---

## 12. Existing Architecture and Known Risks

- `Player.cs` (961 lines) mixes rules, scoring, Photon RPCs, UI text, chat, data recording, WebGL URL parsing, the experiment schedule, payment selection and scene loading; there is no separate game-state model.
- Game state is spread across `Player`, `Manager`, cell components and **UI buttons** (`Button.interactable` decides the remainder).
- The practice scene runs renamed **forks** of the core classes (`PlayerAITutorial` differs from `Player` in ~400 lines); scoring exists in three implementations.
- Coupling via static singletons, `GameObject.Find`/tags, and scene-serialized button arguments and scene names.

The dependency map, the three highest-risk coupling points (**cell ID drift**, **duplicated scoring logic**, **UI-dependent game state**) and other fragile points are documented with evidence in [Reference §12–§13](CUT_AND_CHOOSE_REFERENCE.md#12-dependency-map).

**Focused refactoring candidates** (proposals only; each needs owner approval): a pure, engine-free `CutAndChooseRules` shared by real and practice rounds and covered by EditMode tests; stable serialized cell IDs; allocation state held in data rather than buttons; a master-driven round handshake with disconnect handling; credential removal.

---

## 13. Testing and Development Workflow

**Tests and CI** [Verified]: no project test assemblies or test scripts (only third-party RTLTMPro tests); no CI (`.github` absent); no build scripts. Verification is manual and needs **two clients** (§13.2). *Tools → Live Game Data Monitor* shows live values in Play Mode.

**Repository**: `https://github.com/LIADN7/FairDivision2d`; default branch `main` (human-controlled); `dev` is the shared integration branch for approved UX batches (branch model and agent rules: `CLAUDE.md`). Generated folders (`Library`, `Temp`, `obj`, `*.csproj`) are ignored.

### 13.1 Environment and Editor access (verified 2026-11-01)

| Capability | Status |
|---|---|
| Repo root | `C:\Users\liadn\OneDrive\StudyProjects\UnityProjects\FairDivision2d` (inside OneDrive; sync can interfere with Unity's `Library`) |
| Unity Editor | 6000.2.10f1 (`C:\Program Files\Unity\Hub\Editor\6000.2.10f1`, modules Web/Windows) |
| Unity CLI | `unity` 1.0.0-beta.5 (`%LOCALAPPDATA%\Unity\bin\unity.exe`) |
| Unity Pipeline | `com.unity.pipeline` **0.9.1-exp.1** installed (commit `b591fc3`; experimental; it also raised `com.unity.nuget.mono-cecil` to 1.11.6). Whether it is stripped from WebGL builds is unverified. |
| Claude ↔ Editor | Official Unity plugin for Claude Code (`unity@claude-plugins-official`, skills only) + Pipeline: `unity status` shows the open Editor `ready`; `unity command "<group> <name>"` drives it. Quote the command name as one argument (unquoted names log Console errors). Use `script eval_file` for C# containing backslashes. |
| Quirks | The Editor refreshes packages/scripts only when focused: use `unity command "script recompile"`. Editing a scene file on disk while it is open triggers a blocking "modified externally → Reload" dialog. |

### 13.2 Test mode (isolation from participants)

Compiled only into the **Editor and Development builds** (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`); Release code paths are the original lines. Verified at runtime in Editor Play Mode and a local WebGL Development build:

| Concern | Test mode | Release |
|---|---|---|
| Photon matchmaking (`Launcher.cs`) | AppVersion **`uxtest_2.42`** (copy of the settings, same App ID and region `eu`) | `_2.42` |
| Unity Services (`DatabaseManager.cs`) | Environment **`development`**, auth profile **`uxtest`**; on failure nothing is saved (no fallback) | `production`, profile `default` |
| Survey redirect (`Config.RedirectToSurvey`) | Suppressed; logs `TEST MODE: survey redirect suppressed` | Opens panel4all |
| Visual marker (`TestModeBadge.cs`) | Red "TEST MODE - not research data" badge, top-right | Not compiled |

**Lobby-return isolation (fixed for test mode 2026-11-04, `cb831a4`):** `Launcher.Awake` runs on every `MainScene` load, including after Home/Leave while still connected (when `Start` skips the connect). It used to set `PhotonNetwork.GameVersion = "0.0.1"`, which rewrites the AppVersion for every later authentication (`0.0.1_2.42`). Editor and Development builds now set the test version there too (shared `TestGameVersion` constant), so test clients stay on `uxtest_2.42`. Verified at runtime: fresh launch, Home → lobby (`connected=True`) on both clients, then rejoining a room with both clients. **Release is unchanged** (`#else` keeps the original line): participants who return to the lobby while connected still move to `0.0.1_2.42`, now a pool without test clients. Whether that production behaviour should change is an owner decision. Safe practice: still confirm `uxtest_2.42` in the room before a test. Batch 6 investigation (2026-11-07, test mode only, no production connection): see §17 "Release lobby-return AppVersion".

**Two-client recipe:** Editor Play Mode (from `MainScene`) + a WebGL Development build served on `127.0.0.1` (the project uses Brotli without decompression fallback; a plain local server needs a build with compression temporarily disabled, then restore Brotli and confirm `ProjectSettings` is unchanged). Use test URL parameters such as `?userID=TEST_x&surveyID=TEST_y`. Browser-automation limits: the WebGL player pauses when its tab is hidden (`runInBackground` off); keystrokes do not reach Unity's legacy `InputField` (set the name via `unityInstance.SendMessage("NameField","set_text",…)` from a test page); clicks need a press-and-release with a small move. Never press Next simultaneously on both clients (known deadlock). The first client to join a room is Player 1 (master); join with the Editor second to make it Player 2 (its overlay colours, record and region values can then be read exactly via `script eval`). Saving a script while in Play Mode makes Unity stop Play Mode and recompile, which drops the Editor client, so finish code edits before a run. Leave a stale room (reload the page, stop Play Mode) before restarting a test, or the next client joins it with the wrong role.

### 13.3 Release checklist (participant builds)

Not yet performed or verified on any build. Before deploying to participants:

1. Build with the **Release** profile (`Assets/Settings/Build Profiles/Web - Desktop - Release`) and **Development Build OFF**. The current Editor WebGL platform setting has Development Build **ON**, and the previously deployed itch.io build showed a "Development Build" label — a Development build now runs in **test mode** (separate Photon pool, `development` data, no survey redirect).
2. Launch the release build once (without real participants): the **TEST MODE badge must be absent** and the browser console must **not** contain `TEST MODE:` lines.
3. Confirm production services: Photon AppVersion `_2.42` and Unity Services environment `production` (e.g. via a short check on a non-public URL before announcing).
4. Never publish a Development or test build to participants, and never reuse a test build URL for recruiting.
5. Record the build profile, commit hash and deployment date with the research team.

---

## 14. Research-Critical Invariants

Authoritative list: [Reference §14](CUT_AND_CHOOSE_REFERENCE.md#14-research-invariants) — split into owner-stated requirements, code-confirmed behavior to freeze, and open owner decisions. It covers selection rules, subjective valuations, experimental conditions, round progression, multiplayer allocation consistency, result calculation, data collection, reward calculation and participant-facing texts. Do not change any of them, including during UX work, without research-owner approval.

---

## 15. Verified Facts, Unknowns and Open Questions

**Verified highlights:** 1-2-1 allocation; own-valuation percentages; region mapping; round schedule; payment formula; data keys and fields; all active lobby buttons start with practice; no disconnect handling; no automated tests.

**README claims not matched by code:**
- "The Last Diminisher (3 players)" — no implementation found.
- Round Robin exists (`RRScene`, `RRScripts`) but is unreachable from the active lobby and was not analyzed.
- "If cuts overlap, a predefined selection process resolves the division" — the code always applies 1-2-1; the hook (`isIntuitive`) is a stub.

**Unknown:** deployed WebGL host and which build profile was used (it showed a "Development Build" label); whether winnings are real currency; how Cloud Save records are exported and joined to survey IDs; whether anonymous sign-in reuses a player ID in the same browser (possible key overwrite on repeat play); whether cell keys always match between clients (they matched — keys 0–473 — in the 2026-11-01 two-client run).

**Open questions for the research owners:**
1. Should the Player 1 role (first chooser, receives the remainder) and valuation profile 1 stay fixed per participant for all rounds, or be counterbalanced?
2. Is reusing the identical map and valuations in all six rounds intended?
3. In the `Info` conditions, should opponent valuations stay hidden during the choose phase (current behavior)?
4. Should a drawn cut be required, and should empty regions be prevented?
5. Should all three lobby rooms start with practice, and is a maximum of three concurrent pairs expected?
6. Is the agreement toggle meant to appear only in chat conditions (it lives under `ChatLayer`; its label is inactive in the saved scene)?
7. Confirm the payment formula and currency.
8. What should happen on disconnect or Home (currently the session ends for both and the round is not saved)?

---

## 16. Source References

| Topic | Path |
|---|---|
| Algorithm reference | `docs/CUT_AND_CHOOSE_REFERENCE.md` |
| Rules, RPCs, scoring, schedule | `Assets/Script/Player.cs` |
| UI state, end panel, see-other toggle | `Assets/Script/Manager.cs` |
| Cell values/status/shading | `Assets/Script/PointOfState.cs` |
| County totals/titles | `Assets/Script/CutAndChooseClasses/SumSquares.cs` |
| Line drawing / classification | `Assets/Script/RefactoredUI/LineDrawingManager.cs`, `Assets/Script/RefactoredUI/SquareLineCalculator.cs` |
| Conditions, survey redirect | `Assets/Script/Config.cs` |
| Lobby / pairing | `Assets/Script/Launcher.cs` |
| Scene exits | `Assets/Script/LoadScene.cs` |
| Data record / Cloud Save | `Assets/Script/CutAndChooseClasses/CutAndChoosedatabase.cs`, `Assets/Script/DatabaseManager.cs`, `Assets/Script/ChatMessage.cs` |
| Participant data, winnings | `Assets/Script/PlayerData.cs`, `Assets/Script/CutAndChooseClasses/Utils/RandArrays.cs`, `Assets/Script/EndGamePlayerDataManager.cs` |
| Practice | `Assets/Script/AITutorial/*.cs` |
| URL parameters (WebGL) | `Assets/Plugins/MasterURL.jslib` |
| Scenes | `Assets/Scenes/MainScene.unity`, `CutSceneAITutorial.unity`, `CutScene.unity`, `EndGameFlow.unity` |
| Prefabs | `Assets/Objects/*.prefab`, `Assets/Objects/CutUtilsObj/RandArrays.prefab` |
| Packages / versions | `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset` |
| Project description | `README.md` |

---

## 17. Current Status and Handoff

**As of 2026-11-07.** UX Batch 6 including its follow-up (partner-exit notice; rounds 3–4 randomization check; session-state reset that fixes replay after a lobby return) is implemented on **`feat/ux-batch-6`** (from `dev` `9103593`), pushed, **not merged — awaiting owner review** (see "UX Batch 6" at the end of this section). UX Batch 5 including its final fixes was merged into `dev` through PR #2 (merge commit `9103593` of `feat/ux-batch-5` `d50d7dd`; the remote feature branch has since been deleted). UX Batches 1–4 are accepted by the owner and merged into the shared integration branch **`dev`** (created from `AITutorial` `d1cc192`; Batch 1: merge commit `44772cb` of `feat/ux-batch-1` `56dfd5b`, which contains `tooling/unity-pipeline`; Batch 2: merge commit `0f3f99d` of `feat/ux-batch-2` `d78dd4c`; Batch 3: merge commit `8b29582` of `feat/ux-batch-3` `663c56f`; Batch 4 including its follow-up and final UI fixes: merge commit `a28656e` of `feat/ux-batch-4` `a39b598`, approved 2026-11-04). Start new batches from `dev`. `main` is untouched and human-controlled; before any `main` integration a full regression run (including six rounds) and Release validation (§13.3) are required. Owner priority: **player UX with minimum regression risk; no general refactoring.** Agent Git rules: `CLAUDE.md`.

**UX Batch 1 — done** (IDs from the owner's UX report; display-only, no scoring/networking/saving changes):

| ID | Change | Runtime evidence (two-client smoke test, test mode) |
|---|---|---|
| UX-002 | Hebrew notes show numbers/% and brackets correctly (`Manager.PrepareRtlNote`; tutorial intro text "50%" fixed) | PASS — "(…50%)", colour legend, "38.1%" results in Editor and WebGL |
| UX-008 | Next moved away from Cut/Submit (top-right) | PASS |
| UX-009 | End-of-round results shown with one decimal; stored/payment values unrounded | PASS — shown 48.3%, stored 48.27935 (read back from `development`) |
| UX-011 | Round label larger, bold, readable | PASS |
| UX-015 | Lobby panel (title, 3 steps) and "Room N (k/2 players)" labels | PASS — occupancy updated live |
| UX-020 | Notes auto-size to stay inside the note paper | PASS |
| UX-023 | Readable loading text; clearer messages; room-full message | PARTIAL — room-full path untested |
| UX-027 | Name field accepts digits and underscores | PARTIAL — value set programmatically (browser typing limit); real keyboard untested |

Also verified in that run: practice vs PC on both clients, 1-2-1 selection across clients with identical region maps, round 1 → round 2 progression, Cloud Save writes only to `development`.

**UX Batch 2 — done** (accepted and merged into `dev` 2026-11-02, merge commit `0f3f99d`). Owner-approved wording; details and old/new texts in the batch report.

| ID | Change |
|---|---|
| UX-005 | Help Page: dark panel, fixed text box (auto-size 22–30), Hebrew title "הוראות המשחק" (legacy `Title` kept hidden; `HelpPageCountdown` still writes to it), "חזרה למשחק" button calling the existing `HelpPageBT` `ShowAndHide`. Auto-open timing/frequency unchanged. "50%" stored as `%05` (was shown "%50"). |
| UX-018 | Approved Help text in both scenes; practice copy has its own H1/H4/H6 wording ("המחשב"); H7 (identical cuts) removed. |
| UX-017 | Practice Step0 text, "המחשב" in Step1 panels, dark tutorial text, on-screen widths at 960×600, "סבב תרגול" label (display only). |
| UX-014 | Hebrew labels via the TextRTL pattern (Submit/Next/Exit/Send/Chat/placeholder/agreement, EndGameFlow "שלח"/"יציאה"); notes "בחרו חלק אחד", "אדום: 30.1%", "קיבלתם:"; EndGameFlow heading "הזכייה שלכם:". |
| Fix | Closing the Help Page re-activated `SeeOtherPlayerBT` after the cut; in Info rounds one click showed the opponent's values during choosing and raised `CountPlayerSeeOther` (reproduced in test mode). Now `ShowAndHideOnClick` restores pre-open states and `Player.getOtherView` returns unless see-other is enabled and the player has not cut (both mirrored in the practice forks). |

Verified in test mode (Editor + WebGL Development): practice on both roles, full six-round run to `EndGameFlow` with suppressed redirect, region values identical to a pre-change run with the same cuts, 1-2-1 unchanged, saved record schema unchanged.

Note for agents: all Hebrew TMP text uses the `segoeui SDF Hebrew` material, whose face colour is green (0.55, 1, 0.14) — vertex colours are multiplied by it (white text renders green). Digits/punctuation come from the TMP fallback font.

**UX Batch 3 — done** (accepted and merged into `dev` 2026-11-02, merge commit `8b29582`). Owner-approved wording; presentation only (no RPC, scoring, data or condition changes). Both scenes and the practice forks.

| ID | Change |
|---|---|
| B3-A | `CutView` shows "wait for the other player's cut" only while that cut is missing (fixes the stale note for the second cutter). Display-only turn label (`TurnLabelPlate`/`TurnLabelRTL` under `ViewAfterCut`) derived from role and `chooseNum`, updated in `setAllState`/`Choose`/`GetValues`. Approved turn notes ("המחשב" in practice). The final breakdown stays visible: `SetNoteForOtherPlayer` ignores notes once `chooseNum >= 3`, and the second chooser no longer writes a local note after `GetValues`. |
| UX-003 / UX-006 / UX-024 | Hebrew TMP label per region button (`buttonsValueTextRTL`; legacy `Text` kept hidden and still written): "חלק N - צבע" + "שווה לכם: x.xx%" (two decimals, display only), taken/remainder states, X/O no longer shown. Cut readouts "הצד האדום/הירוק" (precision unchanged), Info readouts "שווה לשחקן השני:" (F5). Value caption under the buttons (choose phase). Lighter blue button and subtler disabled tint. |
| B3-B / B3-C | `CutBT` is a rounded text-first button (built-in UISprite, Help-page cyan), outside the map drawing area and the region-button slots; Button/OnClick unchanged. |
| UX-022 | County labels darker with a white `Outline`; Hebrew legend title and levels; `EndGameStatusObject` centre-anchored (no note overlap at 960×600). |

Verified in test mode (Editor 1600×900 + WebGL Development 960×600): practice in both roles, round 1 (Player 2 cuts first), round 2 (Player 1 cuts first), round 3 Info see-other readouts; region values, ownership and the Player 2 record consistent across clients; saves only to `development`. Not run: rounds 4–6, Chat, EndGameFlow.

**UX Batch 4 — done, merged into `dev` (`a28656e`)** (`feat/ux-batch-4`; owner-approved scope and wording; both scenes and the practice forks):

| ID | Change |
|---|---|
| Shading fix (`75c54b0`) | `createSumRGYB` passes the local player index `j` to `setSpriteStatus` instead of the literal `1` (`Player.cs`, `PlayerAITutorial.cs`). Player 2 now keeps its own heat shading after both cuts. Colour only. |
| UX-001 | Approved instruction "גררו עם העכבר קו ישר על המפה. / אפשר לשרטט מחדש עד שתלחצו על אישור החלוקה." appended to the initial cut note (50% text unchanged). Cut-status label `CutStatusPlate` (copy of the turn-label style, above `CutBT`): "שרטטו קו על המפה" → "לחצו על אישור החלוקה כשתסיימו" → "ממתינים לחיתוך של השחקן השני" ("…של המחשב" in practice). Derived from the line state, `Manager.isCut` and `isAllTrue`; hidden when both cuts exist, while viewing the other map and (practice) while the tutorial locks gameplay. Tutorial steps and timing unchanged. |
| UX-019 | Before the first line, the red/green readouts show "שרטטו קו כדי לראות את השווי" (also in the Info see-other readout). Values, precision and the 50 ms update are unchanged; after the own cut the submitted split is shown. |
| UX-021 | Display-only outline behind each cut line (white behind black, black behind white). Ownership label `LineOwnerPlate` under the value caption after both cuts, built from the actual line colours and naming only lines that are shown, e.g. "הקו השחור: החיתוך שלכם / הקו הלבן: החיתוך של השחקן השני". Wording chosen by the agent: **needs owner confirmation**. |
| UX-026 | Line start ignores presses whose topmost UI hit is interactive or inside the open Help Page (`inputBlockingPanels`); county labels and notes still start a line. |
| UX-004 | Investigation only, nothing changed: see [Reference §2](CUT_AND_CHOOSE_REFERENCE.md#2-line-drawing-validation-and-cell-classification). Owner decision needed. |

Verified 2026-11-03 in test mode (Editor 1280×720 as Player 2 + WebGL Development 960×600 as Player 1; both `uxtest_2.42`, `development`/`uxtest`):
- Practice, then rounds 1–4 (Normal, Normal, Chat, Chat) and round 5 (InfoAChat) up to the choose phase.
- Player 2's overlay matches its own valuation in all 375 distinguishing cells before and after both cuts (0 matching Player 1).
- Info pre-cut see-other shows Player 1's valuation and restores; after the cut, the button is gone and `getOtherView` is a no-op (`CountPlayerSeeOther` 2).
- On both clients, region values for five rounds equal an independent recomputation from the transmitted cuts.
- 1-2-1 sequence and results are consistent; round 1–4 records were saved to `development`, and Player 2's round-1 record read back unchanged (schema unchanged).
- UI guard: Help Page drags, the see-other edge inside the bounds and the "?" button drew nothing; drawing from a county label, redraw, and the post-submit drawing lock work.
- Not run: round 6, EndGameFlow, a Release build, 1600×900 at runtime (edit-mode captures only).

**UX Batch 4 follow-up (2026-11-04, owner-approved, same branch)** — commits `cb831a4` (Photon test isolation), `0bba188` (instruction and waiting UX), `349f6e2` (Help Page lines):

| Item | Change |
|---|---|
| One instruction surface | `UpdatePhaseInstruction` (Player and practice fork) replaces the separate cut-status and turn labels. One dark banner at a time: cut phase above `CutBT` ("שרטטו קו על המפה" / "לחצו על אישור החלוקה"), choose phase under the region buttons ("בחרו חלק אחד" / "בחרו שני חלקים" / "בחרו חלק נוסף"). Derived from the line state, `Manager.isCut`, `isAllTrue`, role and `chooseNum`; hidden while the practice tutorial locks gameplay (refreshed from `TutorialFplanManager.SetGameplayLock`). |
| Note paper | Shows the unchanged 50% guarantee plus "אפשר לשרטט מחדש עד שתלחצו על אישור החלוקה.", the 50% split note, system messages and the result breakdown. Turn/waiting notes are no longer written; `SetNoteForOtherPlayer` keeps its RPC payload but shows nothing. The paper (`NoteObject/RawImage`) hides while empty (`Manager.SetNotePaperVisible`). 1-2-1, personal values and 50/50 remain in the Help Page, the value caption and the tutorial. |
| Waiting overlay | `ViewItems/WaitingOverlay` (last child): tint 35% plus a centred message over the map ("ממתינים לחיתוך של השחקן השני" / "ממתינים לבחירת השחקן השני", "המחשב" in practice), shown only while the other player must act. No raycast targets; non-blocking `CanvasGroup`. Hidden with `ViewItems` while Help is open. The message plate has its own canvas with `overrideSorting` and an order one above the root canvas (1 in `CutScene`, 3 in `CutSceneAITutorial`, whose root canvas is order 2) because `MapBoundaries` (32% white sprite) and cut lines are world objects at z = 0, in front of the UI plane (z = 5). |
| Help Page and lines | The Help Page hides `States`, which hid the lines but not the Batch 4 outlines. The outline holder is now a sibling of the line manager, and lines and outlines use `forceRenderingOff` while a blocking panel is open. |
| Info vs. buttons | Instruction plates: flat dark banner (Help Page style). Value caption and line-owner box: flat pale, regular weight. Buttons unchanged. |
| Tutorial texts | Step0, Step1 P1/P2, Step2 shortened with the same meaning (straight cut, 50/50 by own values, goal sentence kept verbatim, "בסיום לחצו על אישור החלוקה."; 1-2-1 with "המחשב"; result by own values). |

Verified 2026-11-04 in test mode (WebGL Development 960×600 as Player 1 + Editor as Player 2):
- Photon checks A–C (fresh launch, lobby return, rejoin).
- Practice and round 1: instruction texts, overlay shown only while waiting (both roles), Help over lines (lines and outlines hidden, restored after closing), region values equal to the independent recomputation.
- A local raycast probe showed the overlay is never hit.

Follow-up fixes were re-verified in a single-client Editor practice session: refresh on tutorial unlock, the plate drawn above lines and the map box, and the overlay hidden under Help. Not verified at runtime: chat input during a waiting state (no Chat round reached) and the Release build.

**UX Batch 4 final UI fixes (2026-11-04, owner-requested, same branch):**
- **Result box over the Help Page:** `EndGameStatusObject` ("You get total: …") was a direct child of `Canvas`, outside every layer the Help Page hides. It now lives under `ViewItems`, placed just before `WaitingOverlay`. Its on-screen position is unchanged (both are centre-anchored), and the `Manager` references are unchanged. Help hides it and restores its previous state on close. Value and calculations are untouched.
- **Waiting message under the cut lines:** the plate canvas had been saved with `m_OverrideSorting: 0`. The `overrideSorting` setter is ignored while the overlay is inactive, so the earlier fix never reached the scene file. It is now set through the serialized property in both scenes.
- **Verified in test mode** (WebGL Development as Player 1 + Editor as Player 2, practice and round 1):
  - Help at start, after drawing, during selection and after the result: nothing but the Help button is drawn outside the Help Page.
  - The result returns with the same value after repeated open/close.
  - The waiting message stays above both cut lines and outlines at 960×600 and 1600×900.
  - Region values equal the independent recomputation.

**Known issues / not yet verified:**
- **Player 2 heat shading (history):** confirmed 2026-11-02 in test mode. After both cuts, Player 2's heat shading switched to Player 1's valuation (choose phase and result), in Normal, Info and practice; Player 1 was unaffected. Cause: `createSumRGYB` called `setSpriteStatus(status, 1)` (`Player.cs`, `PlayerAITutorial.cs`), while `PointOfState.setSpriteStatus` treats `iPower == currentPlayerNum` as "own values" since `45bb1e8` (2026-07-31). Region percentages, scores, saved records and payment always used `getMyVal(own)`. **Fixed in `75c54b0`, merged into `dev` (`a28656e`); not yet in `main`.** Participants who played builds containing `45bb1e8` saw Player 1's shading as Player 2 during choosing; their stored numbers are correct, but the information condition differed. How to treat that data is an open research decision ([Reference §14.3](CUT_AND_CHOOSE_REFERENCE.md#143-open-questions-requiring-owner-decision) Q7, §15).
- **Photon isolation after a lobby return:** fixed for test mode (`cb831a4`, in `dev`; §13.2). Release participants still move to `0.0.1_2.42` after a lobby return while connected (unchanged production behaviour; owner decision).
- UX-004: owner decision 2026-11-07 — no-line submission (100/0, empty regions) stays allowed; drawing is locked once a cut has been sent (`feat/ux-batch-5`, Batch 5 final fixes). Reference §2.
- Chat message list (RTLTMPro, Batch 5): a purely English message with punctuation can be shown with its words reordered (e.g. "Hello, I agree." shown as "I agree. Hello,"), and long English lines wrap out of order. Hebrew and Hebrew with embedded English/numbers display correctly; the stored and transmitted text is unaffected. Not fixed (outside the approved scope).
- Cut readouts, "You get total" and the round label are still English legacy `Text` (region buttons are Hebrew since Batch 3). Help Page images still show English "Low/High".
- `EndGameFlow` intro text: was clipped at the top at 960×600; on `feat/ux-batch-5` it auto-sizes and fits (UX-010).
- Room-full message, real keyboard name entry and **any Release build** are unverified. Release verification (§13.3) is required before any participant deployment.
- Chat while the waiting overlay is shown: verified on `feat/ux-batch-5` (see Batch 5). Typing: since the Batch 5 final fixes the WebGL chat input is a native HTML input over the Unity field (correct Hebrew while typing). Where that input cannot be used (browser fullscreen — Unity makes the canvas itself the fullscreen element — and the Editor), the Unity field is typed into directly; since 2026-11-07 it shows an RTL-correct TMP copy of the typed text with a caret mark (`TypedTextRTL`) whenever the text contains Hebrew (see below).
- Exposed historical service-account credentials in `UnityCloudDataScraper.cs` still need owner review/rotation (§11).
- Open research questions: §15.

Test-tool notes: WebGL clicks need a hover before the press (Unity reads the last mouse-move position, so a bare press can start a line at the old cursor position); Editor `capture game_view` must save under the project and lands in `Assets/Temp` — move the file out and delete that folder via `AssetDatabase.DeleteAsset`.

Test-tool note (Batch 4): the WebGL canvas can be sampled with `gl.readPixels` inside a `requestAnimationFrame` callback (the drawing buffer is not preserved otherwise). Cell screen positions at 960×600 are `x = 480 + wx·600/11.4`, `y = 300 + wy·600/11.4` (bottom-left origin).

**UX Batch 5 — done, merged into `dev` through PR #2 (`9103593`).** Presentation only; no RPC, scoring, payment, condition, counter or saved-field changes. Files: `CutScene`, `EndGameFlow`, `Manager`, `Player`, `EndGamePlayerDataManager`. The practice scene is unchanged (its chat and see-other are never active).

| ID | Change |
|---|---|
| UX-012 | `ViewItems/ValueOwnerPlate` (Help-page banner style, above the cut readouts): "מוצגים הערכים שלכם" (dark blue) / "מוצגים הערכים של השחקן השני" (dark red). Driven by `Manager.isShowView` (`UpdateValueOwnerLabel` in `Start`, `changeSeeOtherPlayerBT`, `initialSeeOtherPlayerBT`, `setViewAfterCut`). Shown only while `SeeOtherPlayerBT` is active and see-other is enabled, so it disappears with the own cut; hidden under Help with `ViewItems`. |
| UX-013 | Chat heading "צ'אט עם השחקן השני" and info line "אפשר לשוחח עם השחקן השני." (`ChatLayer` is active only in Chat conditions). Placeholder, "שליחה" and the agreement label unchanged. `ChatLayer` is now the last child of `ViewItems`, drawn above the waiting tint (neither takes raycasts). |
| UX-031 | Messages in `ChatLayer/ChatMessagesRTL` (TMP, RTL) through the existing RTLTMPro conversion (`RTLSupport.FixRTL`): own lines "אני:" right-aligned with a cyan label, other lines "השחקן השני:" left-aligned with a yellow label, white text (runtime material instance with a white face; the shared Hebrew material stays green). Chronological, last 10 as before; older lines are left out (not clipped) when they do not fit; a single over-long message shrinks. Sender from the call site (`sendMessage2` vs the RPC) in a display-only list parallel to `chatHistory`; `ChatMessage`, `ChatHistory`, the RPC payload and the 200 cap are unchanged. The legacy `Explanation` text is hidden but still written. Timestamps are no longer displayed (they were local receive times). |
| UX-010 | `EndGameFlow`: heading "התגמול שלכם", the existing `PlayerData` amount as "X.XX ₪" (display rounding to at most two decimals), "התגמול חושב לפי סבב אחד שנבחר באקראי." and, when the drawn index is inside `RandArrays`, "סבב N נבחר לחישוב התגמול." with N = index + 1 (index 0–5 = real rounds 1–6; practice is never added — same mapping as the legacy English "from round N"). Intro text auto-sizes (no clipping at 960×600); Submit moved below; its OnClick is unchanged. |

Verified 2026-11-05 in test mode (WebGL Development 960×600 as Player 1 + Editor 1280×720 as Player 2, both `uxtest_2.42`, `development`/`uxtest`; practice and all six rounds — Normal, Normal, Chat, Chat, InfoAChat, InfoAChat — to EndGameFlow and Finish):
- UX-012 (Editor, InfoAChat): own view — own label, shading 375/375 own. See-other click — other label, dark red, shading 375/375 other, count 1, readouts "שווה לשחקן השני". Help hides and restores it. Back to own — 375/375 own, own readouts, count 2. After the own cut — plate and button gone, a direct `getOtherView` is a no-op (count 2). The next round starts with the own label and count 0. WebGL shows both states. Normal and Chat rounds: plate off.
- Chat: messages sent while the sender waited (Player 2 waiting for the cut; Player 1 waiting for the choice) were delivered and shown as "אני" / "השחקן השני" on the correct sides, in order, with Hebrew, Latin words, numbers and punctuation readable. The overlay stayed on; Editor raycasts at the input, Send and the agreement toggle hit those controls first. `ChatHistory` keeps the original text and nicknames. Agreement toggle → `isAgreed: true` in `CutAChoose_2_Chat`. Chat is empty in the next round (unchanged).
- UX-010: Player 2 index 3 → "סבב 4", 57.71 ₪ shown; stored 57.7137566 = independent recompute from `RandArrays[3]` 63.4758377. Player 1 shows 37.65 ₪ for the same round (recomputed from its 50.1012% result). Finish saved `PlayerData` and the redirect was suppressed.
- UX-019 (code unchanged): placeholder in practice and real rounds before a line (both clients, also in the see-other readout); values after drawing, redraw and Help open/close.
- Found and fixed during the run (`0d21ed0`): with many messages the oldest line overlapped the info line (height was under-measured). Re-verified in the Editor: 7 of 11 messages shown, 313.8/330 px.
- Not verified: real-mouse and keyboard input on WebGL (the Browser pane was hidden, so the player ran at ~2 fps; WebGL actions were sent with `unityInstance.SendMessage` to the same methods the buttons call); runtime capture at 1600×900 (edit-mode captures at that size mirror all Hebrew TMP text — a capture artifact; layout checked there); Release build.

**Incident (2026-11-05):** the first Batch 5 WebGL build (`unity command "build run"` with only `--profileName "Web - Desktop - Development"`) was **not** a Development build. Opened once for about a minute, it signed in anonymously to Unity Services (`production`) and connected to the Photon lobby (`_2.42`); no room was joined and nothing was saved. Always pass `--options '["Development"]'` and confirm the `TEST MODE:` console lines and the badge before joining. The same command also rewrote `m_WebGLTextureSubtarget` in the Development build-profile asset (restored with `git checkout`).

**Batch 5 final fixes (owner-approved 2026-11-07, same branch):**

| Item | Change | Root cause |
|---|---|---|
| Hebrew while typing (UX-013) | `RtlChatInputWebGL` on `ChatLayer/MessegeInputField` + `Assets/Plugins/RtlChatInput.jslib` (same plugin pattern as `MasterURL.jslib`): in WebGL builds a native HTML `<input>` is placed exactly over the Unity field (repositioned from the field's screen rectangle; `dir` rtl when empty, `auto` when typed). The browser does bidi, caret, selection, paste and IME. Each edit copies the raw string into `InputField.text`; Enter calls the field's own `onSubmit` (`Player.sendMessage2`); clearing the field after sending clears the HTML input. Unity's keyboard capture is released only while the HTML input has focus. Hidden under Help (component disabled with `ViewItems`) and in browser fullscreen. No change in the Editor. | The legacy uGUI `InputField`/`Text` has no RTL support. RTLTMPro only offers a `TMP_InputField` wrapper whose caret and mid-text editing are not bidi-aware, so it was not used. |
| Legend (UX-012) | `Manager.legendTitleRTL` (wired in both cut scenes) follows `isShowView` in `UpdateValueOwnerLabel`: "שווי המשבצת עבורכם" / "שווי המשבצת עבור השחקן השני". Title box widened 700 → 960 so both wordings render at 52.6. | The legend text was static. |
| Drawing lock (UX-004) | `LineDrawingManager` / `LineDrawingManagerAITutoria.LockDrawingAfterCut()` sets a local `cutSent` flag checked by `CanDrawLine`; called from `Player.CutView` after the `Cut` RPC (two-player branch only) and from `PlayerAITutorial.CutView`. | A no-line cut never sets `isSubmitted`, the only drawing gate; in practice `Manager.isCut` is set only after the computer's cut. |

Verified 2026-11-07 in test mode (WebGL Development 960×600 as Player 1, verified `TEST MODE` strings in the build and `uxtest_2.42` / `development` / `uxtest` in its console before joining; Editor as Player 2):
- Typing (WebGL, real browser keyboard input into the HTML input, InfoAChat round 5, while Player 1 was waiting): "אני מציע 50-50, אתה מסכים?" displayed in correct order while typing; caret moves (Home, ArrowLeft ×9) and an insertion in the middle ("בערך ") worked; Backspace restored the original; " (Donegal 30%)" appended in correct bidi order; Enter sent it; the field cleared. Player 2 received the identical string (`exactMatch`, logical code points U+05D0 U+05E0 U+05D9…), stored unchanged in `ChatHistory` with sender `B5fix2_Web`. English "Hello, I agree." typed and sent with a real click on the Unity Send button. Help hides and restores the HTML input. Clipboard cut/paste could not be exercised (the embedded browser blocked clipboard writes) — UNVERIFIED.
- Legend: Player 2 (Editor, Info round 3) own → other → Help open/close → own → after the cut, legend always matching the shading (375/375 cells); `CountPlayerSeeOther` 0 → 1 → 2 as before. Player 1 (WebGL) shows the other wording with the plate. Normal and Chat rounds: own wording.
- Drawing lock: Player 2 practice with a line and without a line, round 1 without a line, round 2 with a line — `CanDrawLine` false after the cut, no line after a no-line cut, sent cut = displayed cut, `PlayerCutValues` unchanged (100/0 and 36.52/63.48), Player 2's round-2 result 63.4758377 identical to the earlier Batch 5 run. WebGL (real mouse): after Player 1's no-line cut a drag drew nothing (100/0 kept); control drag before the next cut drew a line (51.417/48.583); after that cut a redraw drag changed nothing, and Player 2 recomputed exactly 51.417/48.583 from the received cut.
- Test incidents (test data only): an early Player 2 cut sent before `InitSquares` arrived (the harness bypassed the hidden Cut button) — that session was left via Home; a later WebGL client dropped from the room (Photon timeout while the hidden pane throttled the page; no disconnect handling, Reference §15) — the run was restarted.

**Batch 5 fix — Hebrew invisible while typing in WebGL (2026-11-07):**
- Symptom (owner): in WebGL, typed Hebrew was not visible while English was; sent Hebrew displayed correctly.
- Root cause (reproduced in a WebGL Development build): the legacy chat `InputField` text uses Unity's built-in `LegacyRuntime` font. In the Editor it falls back to the OS font (Hebrew shown, reversed); a WebGL player has no OS fonts, so Hebrew glyphs are missing and render blank. Visual only — the field value and the sent string were intact. The Batch 5 HTML input (windowed WebGL) was not affected (real Hebrew key events showed correctly); the legacy field is what a participant sees in browser fullscreen (by design the HTML input is hidden there) and in any build without the HTML input (e.g. builds made before `649a62d`).
- Fix: `RtlChatInputWebGL` now also drives `MessegeInputField/TypedTextRTL` (TMP, copy of the RTL placeholder style): when `InputField.text` contains Hebrew it shows an RTL-correct copy (RTLTMPro conversion) with a `|` caret mark at `caretPosition` while focused, and makes the legacy text, caret and selection transparent. Pure English keeps the legacy text. Display only; `InputField.text`, the send pipeline, `ChatMessage`/`ChatHistory` and RPCs are unchanged.
- Verified in WebGL (test mode): HTML input path — Hebrew via real key events, English, mixed "שלום abc 50-50, מה דעתך?", Enter send, received string identical on Player 2, history display; second consecutive Chat round still has exactly one HTML input. Unity-field path (HTML input hidden as in fullscreen) — real keystrokes "שלום abc 50" visible with caret mark, Backspace, Home/Right and insertion in the middle ("שליום"), Enter via the field's own onSubmit, received string identical; English-only "hello ok" unchanged. Editor: preview of the mirror with a mixed string.
- Not verified: real browser fullscreen (the embedded test browser never entered fullscreen), clipboard paste (blocked in the embedded browser), selection highlight in the Unity-field path (hidden while Hebrew is shown).

**UX Batch 6 — implemented on `feat/ux-batch-6`, awaiting owner review (not merged).** Display only: no RPC, scoring, payment, condition, round-schedule, save-timing, matchmaking, timeout or role change. Files: `GameSessionDialogs.cs` (new), `Assets/Objects/SessionDialogs.prefab` (new; one instance in `CutScene` and one in `CutSceneAITutorial` with `practiceScene = 1`), `LoadScene.cs`, `Player.cs`, `PlayerAITutorial.cs`, `RtlChatInputWebGL.cs`. Commits `8a3104d` (UX-016), `0a1f86d` (UX-025), `230d2ca` (UX-030/UX-007).

| ID | Change |
|---|---|
| UX-016 | `SessionDialogs` prefab: own Screen Space - Overlay canvas (order 1000, scaler 570×1100 match height like the game canvas), so it draws above the game canvas, cut lines, waiting overlay, Help Page and tutorial panels (the TEST MODE badge, order 32767, stays on top). `GameSessionDialogs` (`MonoBehaviourPunCallbacks`) shows a **non-modal** notice only for events Photon confirms: `OnPlayerLeftRoom` while this client is still in the room → "החיבור לשחקן השני נותק."; `OnDisconnected` outside a normal room exit → "החיבור לשרת המשחק נותק."; second line "לא ניתן להמשיך בסבב הנוכחי." (practice: "לא ניתן להמשיך במשחק."); button "חזרה למסך הראשי". Real rounds also check at scene start (one player in the room, or not connected). Intent rules: the other player's Home arrives as `LeaveGameForAll` before its Leave event, so this client is already `Leaving` and the departure is ignored; a normal leave reports `OnLeftRoom` in `DisconnectingFromGameServer` (never a notice), a lost connection reports `OnLeftRoom` in `Disconnecting` and then `OnDisconnected`. Return: in the room → the existing `LeaveGame` path; not connected → `SceneManager.LoadScene("MainScene")` (the Launcher then connects as on a fresh start; no automatic reconnection). Home after a confirmed loss uses the same return path. While the notice is shown, `UpdatePhaseInstruction` (both forks) hides the round instruction and the waiting overlay; the card is a click sink (no line under it); Help, chat, Next and its save keep their existing behaviour. `OnMasterClientSwitched` is only logged. |
| UX-025 | Home buttons keep their scene `OnClick` (`LoadScene.LeaveRoomBeforeLoadTheScene` / `...AITutorial`); these now open a modal confirmation; the unchanged exit body (`LeaveRoomNow` / `LeaveRoomNowAITutorial`) runs once, only after "יציאה". Title "לצאת מהמשחק?", buttons "ביטול" (right) / "יציאה" (left). Real rounds add "ההתקדמות בסבב הנוכחי לא תישמר." until this player pressed Next (`Player.HasPressedNext`, display-only flag set in `NextGame`); practice shows the title only (never saved). Backdrop and card are click handlers (`LineDrawingManager` starts no line, nothing below is clicked). The Help Page hides Home, so the dialog never opens over Help. `EndGameFlow` (`LoadTheScene`) and the lobby are unchanged. |
| UX-030 | Reuses the waiting overlay. After this player's Next: "ממתינים לשחקן השני". When `MasterNextGame` arrives (both pressed Next; reaches both clients before `RequestSceneLoad`/`LeaveGame`): "עוברים לסבב הבא"; after the last real round (index 5, read before the master advances the counter): "עוברים לסיום המשחק". `ShowRoundTransition` runs first inside `MasterNextGame`, wrapped in `try/catch`. Practice: "עוברים לסבב הבא". No delay, no handshake, save or RPC change. |
| UX-007 | Integration only: the overlay stays non-blocking, resets with each scene load, is hidden by the connection notice and under Help (unchanged), and lies below both dialogs. `RtlChatInputWebGL` hides the native HTML chat input while a session dialog is open (it is drawn above the WebGL canvas). |

Verified 2026-11-07 in test mode (WebGL Development build `Builds/uxtest_b6` — TEST MODE strings confirmed in the build data, badge shown, console `uxtest_2.42` and `development`/`uxtest` before joining — as Player 1 at 960×600 in a visible Browser pane + Editor 1280×720 as Player 2):
- Single-client Editor practice: exit dialog title only; raycasts at five points all hit the dialog (click handlers); Cancel left all state unchanged; "יציאה" ran the exit once and loaded the lobby with no notice.
- Session 1: practice, then all six rounds (Normal, Normal, Chat, Chat, InfoAChat, InfoAChat) to `EndGameFlow` (Player 1 reward 112.50 ₪ = 30 + (100 − 45) × 1.5 from round 5). Waiting texts before the opponent's cut and during selection unchanged. Round 1: real-mouse line and clicks on WebGL; Player 1's region values recomputed on the Editor from both cuts (29.96 / 38.36 / 24.29 / 7.39) = WebGL's buttons; Player 2's shown values = recomputation; record fields unchanged (18 fields), `SumOfMyPart` 58.4572449 = 20.91078 + 37.5464668; saved once per round to `development`. Exit dialog Cancel in practice, round 1 (with the "not saved" line), after Next (without it) and on WebGL with real clicks (a drag behind the dialog drew nothing). Next waiting text on the first presser; transition text on both (first presser via the RPC, second presser in the same frame); last round "עוברים לסיום המשחק"; one scene load per transition; overlay off in the new scene; `RandArrays` unchanged by practice. Chat round: messages while waiting (both directions, real keyboard Hebrew on WebGL) and after Next; the HTML input hides while the exit dialog is open and returns after Cancel.
- Session 2: intentional Home with real clicks on WebGL and via the Editor (practice and round 1): the other client returns to the lobby, its `OnPlayerLeftRoom` arrives in state `Leaving` and is ignored (no notice).
- Disconnects: (a) silent drop — WebGL tab hidden (player paused) during round-1 choosing: the Editor received `OnMasterClientSwitched` + `OnPlayerLeftRoom` **about 65–72 s** later (hidden between 19:55:10 and 19:55:17 UTC, event 19:56:22.298) and showed the notice 1 ms after the event; on re-showing the tab WebGL reported `OnLeftRoom` (Disconnecting) → `OnDisconnected(ClientTimeout)` and its notice within ~0.2 s. (b) Socket-closing drop — WebGL page reload in practice: Editor event 0.23 s later, notice +4 ms, practice wording, shown above an open Help Page whose "חזרה למשחק" button stayed clickable (raycast). (c) Client-side `PhotonNetwork.Disconnect()` in the Editor: `OnLeftRoom` → `OnDisconnected(DisconnectByClientLogic)` → notice +2 ms. Return buttons: in the room → existing exit path, lobby on `uxtest_2.42`; disconnected → lobby, fresh connect on `uxtest_2.42`.
- After the master left, the Editor became master (`IsMasterClient` true); the instruction and overlay stayed hidden under the notice; no game action was taken.

Limitations (not changed, report only): a client that stops running (hidden/paused WebGL tab) is reported to the other player only after ~65–72 s, because PUN keeps the connection alive in the background for `KeepAliveInBackground` (60 s) before the server times it out; meeting ~15 s would require changing Photon timeout/keep-alive settings or adding a heartbeat (not authorized). Photon's network simulation does not drop packets on the WebSocket transport used here, so a real network cut (cable/Wi-Fi) was not reproduced — UNVERIFIED. Socket-closing departures (tab closed/reloaded, app quit) are reported within ~0.25 s.

**Release lobby-return AppVersion (investigation only, nothing changed):** still present. `Launcher.Awake` (Release branch) sets `PhotonNetwork.GameVersion = "0.0.1"` on every `MainScene` load, which changes `AppVersion` to `0.0.1_2.42` for the next authentication. A fresh start is unaffected (`ConnectUsingSettings` resets it to the settings' empty version → `_2.42`). Runtime order observed in test mode (Editor state log): `LeaveRoom` → `Leaving` → `DisconnectingFromGameServer` → `ConnectingToMasterServer` → *MainScene loads, `Launcher.Awake` runs (0.2 s after LeaveRoom)* → `Authenticating` to the master server ~0.9–1.0 s after `LeaveRoom`. So in Release the master server is authenticated with `0.0.1_2.42`: the client lands in a separate matchmaking pool (room list, occupancy and `Room_N` are per AppVersion), and a returning participant cannot meet a freshly loaded one. Transitions: Home/exit on either client (`LeaveGameForAll` → MainScene), the Batch 6 notice's return while still in the room, and `EndGameFlow` → Home; not affected: page load, return after a local disconnect (not connected → fresh connect). Batch 6 makes the lobby-return path more likely (notice return). Smallest safe correction (owner decision): in the Release branch set the version only when not connected (`if (!PhotonNetwork.IsConnected) PhotonNetwork.GameVersion = "0.0.1";` — `ConnectUsingSettings` still overwrites it, so fresh clients stay `_2.42` and deployed builds stay compatible). Tests before approval: (1) Development build compiled with the Release branch logic but a test version base, two clients, fresh → Home → rejoin, verifying the AppVersion in the `Authenticating` state and that a returned and a fresh client meet in one room; (2) Release build on a non-public URL with two clients, no participants (§13.3), console `PhotonNetwork.AppVersion` after a Home return = `_2.42`, room occupancy visible across a returned and a fresh client; (3) regression: fresh launch, EndGameFlow → Home, practice → round 1.

**Observed during Batch 6 testing (pre-existing, not changed):**
- Cell-key drift after a lobby return (Reference §13.1) reproduced: after six rounds the Editor (not restarted) had cell keys 2844–3317 while a freshly loaded WebGL client had 0–473; the next round's `createSumRGYB` could not find the keys and the round stalled after both cuts (test data only; the session was left via the exit dialog). Reconciled in the follow-up below: the stall needs two clients with different round histories since their last page load. `RandArrays` is likewise not reset across sessions.
- `EndGameFlow` → Home leaves the client `ConnectedToMasterServer` without joining the lobby, so room occupancy labels are not updated (joining still works).
- `GameOverClicked` still writes "השחקן השני ממשיך במשחק..." on the note paper of the player who has not pressed Next (replacing the result breakdown); unchanged.
- Simultaneous Next on both clients (known deadlock) now shows "ממתינים לשחקן השני" on both; the handshake is unchanged.

**Batch 6 follow-up (2026-11-07, same branch, owner-approved):**

| Item | Result |
|---|---|
| Partner-exit notice (implemented) | When the other player confirms Home → "יציאה", the remaining player now sees, in `MainScene`, a modal notice "השחקן השני יצא מהמשחק." / "לא ניתן להמשיך במשחק הנוכחי." / "חזרה למסך הראשי" (white title, darker dim; the connection notice keeps its amber title). Mechanism: the existing `LeaveGameForAll` RPC still takes the remaining client to `MainScene` 0.2 s later (no message can be shown in the game scene), so `Player` / `PlayerAITutorial.LeaveGame` (sender side) calls `GameSessionDialogs.NoteLocalLeave()` and the first line of both `LeaveGameForAll` bodies calls `GameSessionDialogs.NoteLeaveGameForAll(sceneName)`: a static, display-only `partnerExitPending` is set only when the destination is the lobby and this client did not start the leave (Home/exit, the notice's return and the round-6 `EndGameFlow` call all mark the sender). A third `SessionDialogs` instance in `MainScene` (`lobbyScene = 1`) shows it once in `Start`, blocks the lobby controls (dim layer is a raycast target; the button is selected with keyboard navigation off) and only hides it on the button. Statics are cleared whenever a game scene starts and when the lobby reads them. No RPC payload/signature, room, save, role, matchmaking or timeout change. Files: `GameSessionDialogs.cs`, `Player.cs`, `PlayerAITutorial.cs` (one line each in `LeaveGame` and `LeaveGameForAll`), `MainScene.unity` (one prefab instance). |
| Rounds 3–4 condition (verify only, unchanged) | Matches §6.3. `Player.MasterNextGame` runs on the master once both pressed Next; `Config.addRoundNumber()` returns 2 after round 2 and only then `UnityEngine.Random.Range(2, 4)` (int overload, max exclusive → 2 = Info, 3 = Chat) is drawn, once, and sent to all with `RequestSceneLoad(scene, scenario, 2)`; every client applies `Config.createConfig`. After round 3 (`r == 3`) the master resends its own `Config.getScenarioType()`, i.e. the same scenario; `r == 4` → 4 (InfoAChat), reused for round 6. Sessions start from `createConfig(1, 0)` (lobby button and again on the practice exit `RequestSceneLoad(…, 1, 0)`), so every session draws anew. Nothing in the project calls `Random.InitState` (the only PUN script that does, `OnJoinedInstantiate`, is in no scene or prefab). Evidence: Editor harness replaying the schedule against `Config` — draw 2 → Normal, Normal, Info (see=True, chat=False) ×2, InfoAChat ×2; draw 3 → Normal, Normal, Chat (chat=True, see=False) ×2, InfoAChat ×2; 20 000 real `Random.Range(2,4)` calls → 9 986 × 2, 10 014 × 3, nothing else. A temporary probe (never committed) in the test build logged the start state of Unity's generator on three WebGL page loads and one Editor start: four different states, first `Range(2,4)` = 2, 3, 2 (WebGL) and 2 (Editor) — the generator is not seeded identically per load. The record's `ScenarioType` is read from each client's `Config`, set by the same RPC. Repeated Chat in rounds 3–4 is consistent with chance (p = 0.5 per session; the earlier Batch 6 six-round run also drew Chat). |
| Replay after a lobby return (read-only, unchanged) | Reconciled: **confirmed pre-existing bug with a specific prerequisite**, not a harness artifact. `PointOfState.key` is static, never reset, and advances by 474 per `CutScene` load; the `Cut` RPC carries the sender's keys. A new game works only if both clients have loaded the same number of rounds since their last page load. Runtime (follow-up): the same two clients returned to the lobby together and started again without reload → both at keys 474–947, 0 keys missing, the round proceeded (the owner's observation). The earlier failure had Editor 2844–3317 (six rounds, Play Mode not restarted) vs a reloaded WebGL client 0–473 → stall after both cuts; production equivalent: a participant who returns to the lobby (partner exit/disconnect, `EndGameFlow` → Home) and is then paired with someone with a different history (fresh page or a different number of rounds). `RandArrays` (prefab in `CutSceneAITutorial`/`CutScene`/`EndGameFlow`, `DontDestroyOnLoad`; its "destroy in MainScene" check never runs because it is not in `MainScene`) is never cleared: runtime the Editor kept 1 entry from a finished round into the lobby and the next session. Payment impact: after a returned session with k saved results, the round-6 index 0…5 reads the old entries first, so the reward can come from the abandoned/previous session (Inferred from code; the index → value lookup is unchanged). Smallest fix proposal (needs owner approval, not implemented): on every `MainScene` load (`Launcher.Awake`), reset `PointOfState`'s key counter to 0 and clear `RandArrays` (and the AI fork counters likewise). |
| Session-state reset (implemented, owner-approved) | **Fixes the replay-after-lobby defect above.** Boundary: the Cut-and-Choose room buttons `Launcher.CreateAITutorialGame1` / `CreateGame1` / `CreateTetsGame1` call `Launcher.ResetExperimentSessionState()` right before their existing `Config.createConfig(…, 0)` (which already re-initialises condition and round counter there). At that moment the client is in `MainScene`, not in a room, no map cell exists, no round message can arrive, and the previous session's `EndGameFlow` (payment display, Finish/save) is gone; it runs once per room-button press on each client (idempotent if pressed again). It sets `PointOfState`'s static key counter to 0 (`PointOfState.ResetKeyCounter`), the practice fork's counter to 0 (`PointOfStateAITutorial.ResetKeyCounter`; practice keys never leave the client, reset only to match a fresh page) and empties `RandArrays` (`RandArrays.Clear`). Not reset per round: inside an experiment the counters keep advancing by 474 per `CutScene` load exactly as before (both clients share the same loads), and results keep accumulating for the round-6 draw. Practice cells' `PointOfState` components are disabled in `CutSceneAITutorial` (71 × `m_Enabled: 0`), so only `CutScene` loads advance the real counter. Unchanged: RPC payloads/signatures, cell count, valuations, geometry, regions, roles, schedule, payment formula and draw, saved fields. Files: `Launcher.cs`, `PointOfState.cs`, `AITutorial/PointOfStateAITutorial.cs`, `CutAndChooseClasses/Utils/RandArrays.cs`. |

Follow-up runtime check (Editor 1280×720 as Player 2 + WebGL Development build in the browser pane as Player 1; build checked for the Development-only strings; both logged `uxtest_2.42`, Unity Services `development`, profile `uxtest`, TEST MODE badge; WebGL driven through `SendMessage` on the same methods the buttons call because real mouse clicks did not reach the canvas in the pane this time): Cancel on both (practice) → nobody notified, state unchanged; Player 1 exit in practice → Player 2 lobby notice 0.82 s after the RPC, the exiting client none, Room 1 underneath not clickable (top raycast = `Dim`), after the button Room 1 reachable again; new session without reload → no notice; practice → round 1 and the round-6 `EndGameFlow` path (reached by advancing the master's round counter in the test client) → `partnerExitPending=False`, no notice, also not after `EndGameFlow` → Home; Player 2 exit in round 1 (exit dialog with the "not saved" line) → Player 1 lobby notice 0.28 s after the RPC; WebGL page reload in practice → Editor connection notice "החיבור לשחקן השני נותק." / "לא ניתן להמשיך במשחק.", its return → lobby without a partner-exit notice.

Session-reset runtime check (new WebGL Development build `Builds/uxtest_b6r`, checked for the Development-only strings; Editor = Player 2; both `uxtest_2.42`, `development`, profile `uxtest`, TEST MODE): edit-mode harness on the real `ResetExperimentSessionState` — key 2844 / practice 474 / 6 results → 0 / 0 / 0, second call no change, six new results append normally, no `RandArrays` → no error. **Scenario A (unequal histories):** the Editor played practice + round 1 + start of round 2 and exited (counter 948, practice 474, 1 old result); WebGL reloaded (fresh). On the room buttons the Editor logged `SESSION-RESET … 948 -> 0, practice 474 -> 0, results cleared 1`, WebGL `0 -> 0`. All six rounds then ran: per round the Editor's live keys equalled the keys in Player 1's `Cut` RPC (0–473, 474–947, 948–1421, 1422–1895, 1896–2369, 2370–2843; 0 missing), regions were computed, 1-2-1 completed; conditions Normal, Normal, Chat, Chat, InfoAChat, InfoAChat. Without the reset the Editor would have used 948–1421 against 0–473 in round 1 (the earlier reproduction: 2844–3317 vs 0–473 → stall). `RandArrays` on the Editor: 0 after practice, then 1…6 entries, one per round [46.7472, 24.9071, 52.9740, 48.1413, 30.1115, 57.4349]; at `EndGameFlow` drawn index 3 → winnings 34.71189 = 30 + (48.1413 − 45) × 1.5 (WebGL: 36.13 ₪, "סבב 4"). **Scenario B (equal histories):** both `EndGameFlow` → Home → room button: both `2844 -> 0`, Editor `results cleared 6`; a first attempt stalled in round 1 because of the separate Help/`InitSquares` race below (keys were correct: live cells 0–473, Player 1 sent 0–473); after both returned again (`474 -> 0` on both) round 1 ran with keys 0–473 = 0–473, regions computed, 1-2-1 completed, `RandArrays` = 1 entry (this session only).

**New finding during the session-reset test (pre-existing, not changed): Help page vs `InitSquares` race.** `HelpPageCountdown` opens the Help page 1.0 s after each round scene starts, which deactivates the map cells; `InitSquares` (buffered RPC from the master) collects the cells with `FindGameObjectsWithTag("SquarePoint")` when the RPC arrives, and that call skips inactive objects. Observed: Editor scene start 22:37:40.925 UTC, `InitSquares` 22:37:42.391 (1.47 s later, Help already open) → 0 cells (measured: 474 active with Help closed, 0 with Help open) → empty `squares`/`sumPlayer` → empty `Cut` → `KeyNotFoundException` on the master after both cuts; the round stalls. It can hit a client whenever the master's `InitSquares` reaches it more than ~1 s after its own scene start (master loading slower than the partner). Independent of the session reset (it concerns which cells are found, not their keys). Possible fix for owner decision: collect the cells including inactive ones, or delay the auto-open of Help until the cells are registered.

**Next:** owner review of Batch 6 and its follow-up (wording marked for confirmation: "החיבור לשרת המשחק נותק.", "לא ניתן להמשיך במשחק.", "חזרה למסך הראשי", "עוברים לסיום המשחק") before any merge into `dev`.

Still open owner decisions: the Release lobby-return AppVersion (above and §13.2); disconnect detection latency for paused clients (timeout/keep-alive change or not); whether game input should be blocked under the connection notice (currently not, which keeps Next and its save); the Help page / `InitSquares` race (new finding above; can stall a round); the treatment of pre-fix Player 2 records (Reference §14.3 Q7).
