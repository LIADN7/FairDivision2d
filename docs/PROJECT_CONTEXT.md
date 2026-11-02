# Fair Division 2D — Project Context

> Overview for developers and AI agents joining the project. The full implementation reference for the core algorithm — cells, cuts, regions, selection, synchronization, state machines, dependency map, coupling risks and research invariants — is **[CUT_AND_CHOOSE_REFERENCE.md](CUT_AND_CHOOSE_REFERENCE.md)**. This file does not repeat it.
>
> **Last verified:** 2026-11-02 on `dev` after the Batch 2 merge `0f3f99d` (earlier baseline `1f7b01d`): static inspection plus Editor checks through Unity Pipeline and one two-client smoke test in test mode. **New agent? Start with [§17 Current Status and Handoff](#17-current-status-and-handoff).**
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
- **Model**: RPC-only, peer-symmetric, no room properties; master = Player 1; no disconnect or master-switch handling.
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

**Two-client recipe:** Editor Play Mode (from `MainScene`) + a WebGL Development build served on `127.0.0.1` (the project uses Brotli without decompression fallback; a plain local server needs a build with compression temporarily disabled, then restore Brotli and confirm `ProjectSettings` is unchanged). Use test URL parameters such as `?userID=TEST_x&surveyID=TEST_y`. Browser-automation limits: the WebGL player pauses when its tab is hidden (`runInBackground` off); keystrokes do not reach Unity's legacy `InputField` (set the name via `unityInstance.SendMessage("NameField","set_text",…)` from a test page); clicks need a press-and-release with a small move. Never press Next simultaneously on both clients (known deadlock).

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

**As of 2026-11-02.** UX Batches 1 and 2 are accepted by the owner and merged into the shared integration branch **`dev`** (created from `AITutorial` `d1cc192`; Batch 1: merge commit `44772cb` of `feat/ux-batch-1` `56dfd5b`, which contains `tooling/unity-pipeline`; Batch 2: merge commit `0f3f99d` of `feat/ux-batch-2` `d78dd4c`). Start new batches from `dev`. `main` is untouched and human-controlled; before any `main` integration a full regression run (including six rounds) and Release validation (§13.3) are required. Owner priority: **player UX with minimum regression risk; no general refactoring.** Agent Git rules: `CLAUDE.md`.

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

**Known issues / not yet verified:**
- Stale note: the second player to cut sees "wait for the other player to cut" after both cuts (`Player.CutView` writes it after `setAllState`) — pre-existing.
- End-of-choice note: in real rounds the per-colour breakdown is immediately overwritten by "Player 1 got the last remaining part" / "You got the last remaining part" — pre-existing (observed 2026-11-02).
- Region buttons, cut readouts, "You get total", "Taken By Player" and the round label are still English legacy `Text` set from code (Hebrew needs component swaps; deferred).
- `EndGameFlow` intro text: the first lines are clipped at the top of the screen — pre-existing.
- Room-full message, real keyboard name entry and **any Release build** are unverified.
- Exposed historical service-account credentials in `UnityCloudDataScraper.cs` still need owner review/rotation (§11).
- Open research questions: §15.

**UX Batch 3 backlog** (owner-approved deferrals from Batch 2; plan only, not implemented):

| ID | Issue | Expected improvement |
|---|---|---|
| B3-A | Player 1 may lack a clear instruction when it becomes their turn to choose a region (after Player 2's first pick / at the start of choosing). | Clear, role-aware turn guidance. New or changed note wording is participant-facing: get owner approval of the exact Hebrew text. |
| B3-B | The Hebrew "אישור החלוקה" label on `CutBT` extends outside the confirmation button and approaches the note panel. | Fit the label inside an appropriately sized button. |
| B3-C | The cut confirmation button (`CutBT`, scissors sprite) is visually unclear. | Improve clarity with existing UI assets, without changing the interaction (still calls `Player.CutView`). |

**Next:** a new session starts from `dev` and plans UX Batch 3 (items above plus any owner additions) on a new feature branch. Instruction wording is participant-facing protocol: get research-owner approval before changing it.
