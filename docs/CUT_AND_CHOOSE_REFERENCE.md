# Symmetric Cut and Choose — Technical Reference

> Detailed implementation reference for the core algorithm. For the project overview, experiment schedule, data schema and environment see [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md); this file does not repeat them.
>
> **Last verified:** 2026-11-01 — static inspection on `AITutorial` @ `d1cc192`, plus two-client runtime smoke tests on `feat/ux-batch-1` and (2026-11-02, six rounds) `feat/ux-batch-2` (see PROJECT_CONTEXT §17). Batches 1–3 changed display only, plus the Batch 2 see-other guard (§15). Batch 4 (`feat/ux-batch-4`, 2026-11-03, not yet integrated) adds the Player 2 shading fix (§3), a UI-click guard on line start, line outlines (§2) and cut-phase labels. The rules, sync and persistence described here are unchanged. Evidence tags follow PROJECT_CONTEXT: **[Verified]**, **[Inferred]**, **[Unknown]**. Paths are relative to `Assets/Script/` unless stated otherwise.

## Contents

0. [Three layers of truth](#0-three-layers-of-truth)
1. [Map cells and valuation data](#1-map-cells-and-valuation-data)
2. [Line drawing, validation and cell classification](#2-line-drawing-validation-and-cell-classification)
3. [The four regions](#3-the-four-regions)
4. [The 1-2-1 selection sequence](#4-the-1-2-1-selection-sequence)
5. [Allocation percentages](#5-allocation-percentages)
6. [Roles, host authority and state ownership](#6-roles-host-authority-and-state-ownership)
7. [Photon communication and synchronization](#7-photon-communication-and-synchronization)
8. [State machines: lobby to results](#8-state-machines-lobby-to-results)
9. [Round progression and experiment conditions](#9-round-progression-and-experiment-conditions)
10. [Persistence and result calculation](#10-persistence-and-result-calculation)
11. [Worked numerical example](#11-worked-numerical-example)
12. [Dependency map](#12-dependency-map)
13. [High-risk coupling points](#13-high-risk-coupling-points)
14. [Research invariants](#14-research-invariants)
15. [Edge cases and failure scenarios](#15-edge-cases-and-failure-scenarios)

---

## 0. Three layers of truth

| Layer | Source | Meaning for a change |
|---|---|---|
| **Intended rules** | Research owner (2 players; independent halving; 4 regions; P1 picks 1, P2 picks 2, P1 gets the remainder; scores by own valuation) | The specification. All seven rules are matched by the code (§4, §5). |
| **Code-confirmed behavior** | C# and serialized scene data | What the game currently does, including choices the rules do not specify (straight-line cuts, Player 1 = host, fixed map, visibility timing). Treat as frozen unless approved (§14). |
| **Unverified runtime behavior** | Inferred from code; never executed during documentation | Race conditions, Start-order determinism, Unity event resolution of stale bindings, Cloud Save identity reuse. Must be tested with two real clients before relying on it. |

---

## 1. Map cells and valuation data

**Cells** [Verified] — `PointOfState` ([PointOfState.cs](../Assets/Script/PointOfState.cs)), 474 GameObjects tagged `SquarePoint` in `CutScene.unity`, children of nine county objects carrying `SumSquares`.

| Member | Meaning |
|---|---|
| `myVal1`, `myVal2` (serialized, L10–11) | Value of the cell to Player 1 / Player 2. `getMyVal(i)` returns `myVal1` when `i == 1`, otherwise `myVal2` (L129). |
| `spriteStatus` | 1 Red, 2 Green (cut phase); 1–4 region (choose phase). Initialized to 1 in `Start`. |
| `myKey` | Runtime ID from `private static int key` (L20), assigned `key++` in `Start` (L37). See §13.1. |
| `stateName` | Parent (county) name, copied in `Start`. |
| `myPowerColor`, `otherPowerColor` | Heat-shading intensity, `value / total × 500`, 0 for values below 4 (`setmyPowerColor`, L201). Display only. |
| `setSpriteStatus(status, iPower)` (L145) | Sets status and color; `iPower` chooses whose shading is shown. |

**Valuation data** [Verified from scene data] — every county is uniform (all its cells share one value pair):

| County | Cells | P1 value per cell | P2 value per cell | P1 share | P2 share |
|---|---|---|---|---|---|
| Antrim | 69 | 8 | 4 | 27.94% | 12.83% |
| Armagh | 31 | 4 | 8 | 6.28% | 11.52% |
| Cavan | 38 | 8 | 8 | 15.38% | 14.13% |
| Derry | 42 | 4 | 4 | 8.50% | 7.81% |
| Donegal | 99 | 2 | 2 | 10.02% | 9.20% |
| Down | 58 | 2 | 8 | 5.87% | 21.56% |
| Fermanagh | 39 | 4 | 4 | 7.89% | 7.25% |
| Monaghan | 27 | 8 | 2 | 10.93% | 2.51% |
| Tyrone | 71 | 2 | 4 | 7.19% | 13.20% |
| **Total value** | **474** | **1976** | **2152** | 100% | 100% |

`CutSceneAITutorial.unity` holds an identical copy on `PointOfStateAITutorial` components (same per-county values and totals). Values are never modified at runtime (no assignments to `myVal1`/`myVal2` in any script) [Verified]. Because counties are uniform, any record that stores *(county, status)* per cell — as `playerCutSquares` does — is enough to recompute value shares offline.

**Totals** — `Player.withSec` (L154) sums both players' values over all indexed cells into `sumPlayer[0]` (P1) and `sumPlayer[1]` (P2) on each client.

---

## 2. Line drawing, validation and cell classification

**Input** — `LineDrawingManager` ([RefactoredUI/LineDrawingManager.cs](../Assets/Script/RefactoredUI/LineDrawingManager.cs)), a `MonoBehaviourPunCallbacks` on a scene object that must be named `LineDrawingManager` (it is located by name in `Player.CutView` and `Player.setAllState`).

| Step | Method | Behavior [Verified] |
|---|---|---|
| Gate | `CanDrawLine` (L181) | `!isSubmitted` (practice copy also requires `!TutorialFplanManager.IsGameplayLocked`). `IsMyTurnToDraw` always returns true. |
| Start | `StartDrawing` (L246) | Only if the mouse is inside `mapSize` 8.5 × 6.8 around (−5, 1.5) (scene values). Replaces any previous line. Since Batch 4 (`feat/ux-batch-4`, UX-026): not started when the topmost EventSystem raycast hit is interactive (has a pointer-down/click handler: buttons, toggles, input fields) or lies inside an active `inputBlockingPanels` entry (`HelpPageLayer`). County labels and notes (raycast targets without handlers) do not block. Before this, a press on the part of `SeeOtherPlayerBT` that overlaps the bounds at 960×600, or on the open Help Page, started a line. |
| Drag | `UpdateDrawing` | End point clamped to bounds; every 50 ms `SquareLineCalculator.ColorAllSquaresByLine` recolors all cells and `Player.statusChange` updates the readout. |
| Release | `FinishDrawing` (L326) | Final recolor; `hasDrawnLine = true`. Nothing is sent. |
| Cancel | `Esc` while dragging | Hides the line; cell colors from the last update remain. |
| Submit | `SubmitLine` (L476) | If no line or already submitted → warning and return. Otherwise `isSubmitted = true` and RPC `ReceiveSubmittedLine` (Others) with the coordinates (stored, not drawn). |
| Reveal | `ShowBothLines` (L554) | Called from `Player.setAllState` once both cuts exist; draws P1 black, P2 white. Batch 4 (UX-021): `SyncLineOutlines` (LateUpdate) keeps a display-only outline (white behind black, black behind white, width × `outlineWidthFactor` 2, 0.05 further from the camera) for every line renderer under the manager; outlines live under a holder beside the manager (same parent, so the Help Page hides them with the lines) and are never read by drawing, classification or networking. While a blocking panel is open, lines and outlines also use `forceRenderingOff` (follow-up `349f6e2`). The practice copy does not call `ShowBothLines` (its `GameObject.Find("LineDrawingManager")` misses the object named `LineDrawingManagerAITutoria`); the own line stays visible from drawing and the computer's line comes from `PCPlayerAITutorial` [Verified, pre-existing]. |

**Classification** — `SquareLineCalculator.GetSquareColorByLine` ([RefactoredUI/SquareLineCalculator.cs:16](../Assets/Script/RefactoredUI/SquareLineCalculator.cs#L16)) [Verified]:

```
orient segment so that start.x < end.x (or start.y < end.y if vertical)
cross = (end − start).x · (cell − start).y − (end − start).y · (cell − start).x
status = cross > 0 ? 1 (Red) : 2 (Green)
```

The segment acts as an infinite line through its two points; the cell's `transform.position` (centre) decides. Red = above a non-vertical line or left of a vertical one; points on the line → Green.

**Submission and validation** — `Player.CutView` ([Player.cs:219](../Assets/Script/Player.cs#L219)):
1. Calls `SubmitLine` (result not checked).
2. If `PhotonNetwork.InRoom && PlayerCount > 1`: records `PlayerTimeCut`, `PlayerCutValues {Red: 100 − g, Green: g}` (g = `statusChange()`), `playerCutSquares`, then sends RPC `Cut(Dictionary<int,int> key→status, playerNum)` to All, hides the Cut and see-other buttons (`Manager.setViewAfterCut`).
3. Otherwise shows "Waiting for the other player to connect!"; the line is already locked, the Cut button stays, and pressing it again later sends the cut.

There is **no geometric or value validation** [Verified]: no requirement that a line was drawn, that both sides are non-empty, or that the split is near 50%.

**Submission without a line (UX-004, investigated 2026-11-03, unchanged)** [Verified in test mode unless marked]:
- `SubmitLine` logs "Cannot submit: No line has been drawn yet" and returns, but `CutView` continues: it records `PlayerCutValues {Red: 100, Green: 0}` and sends the current cell statuses (all Red by default) via `Cut`. `isSubmitted` stays false, so the player **can keep drawing after the cut**. Drawing then recolours the local cells and rewrites the red/green button readouts (also during choosing), but changes nothing that was sent or stored [Inferred from code].
- The submitter sends no `ReceiveSubmittedLine`, so neither client shows a line for that player.
- With an all-Red cut by Player 1, the regions are Red (P1 Red ∩ P2 Red) and Yellow (P1 Red ∩ P2 Green), together the whole map; Green and Blue are empty (0 %) but still selectable. Observed in practice: the computer (Player 2) picked the two empty parts and Player 1 received 100 %. With an all-Red cut by Player 2, the empty regions are Yellow and Blue.
- Variant: after a completed line, starting a redraw and pressing `Esc` clears `hasDrawnLine` and hides the line, but the cells keep the colours of the last drag update. Pressing Cut then sends those colours, without a line [Inferred from code].
- Smallest future corrections (owner decision; none implemented): (a) display integrity only, with no outcome change: set `isSubmitted = true` (or lock drawing) whenever `CutView` sends a cut, so no local redrawing happens after a no-line cut; (b) behaviour change: keep `CutBT` non-interactable until `HasDrawnLine()`, which would remove no-line and Esc-variant submissions. That is a protocol decision (§14.3 Q3).

---

## 3. The four regions

When both entries of `playerCut[]` are true, each client independently runs `Player.setAllState` (L315) → `createSumRGYB(me)` (L484), which reads the two cut dictionaries `player1`, `player2` **by the local cell key** and assigns each cell a region [Verified]:

| Status | Color | P1 side | P2 side | Index / button (scene object) | UI label | Data key |
|---|---|---|---|---|---|---|
| 1 | Red | Red | Red | 0 `BackRedValue` | Part 1 (X) | `RedX` |
| 2 | Green | Green | Green | 1 `BackGreenValue` | Part 2 (O) | `GreenO` |
| 3 | Yellow | Red | Green | 2 `BackYellowValue` | Part 3 (O) | `RedO` |
| 4 | Blue | Green | Red | 3 `BackBlueValue` | Part 4 (X) | `GreenX` |

Side effects of `createSumRGYB`: recolors every cell (`setSpriteStatus(status, j)` since Batch 4; before that `setSpriteStatus(status, 1)`), stores per-region cell counts (`AmountOfColor`) in the record. The comment in `PointOfState.setSpriteStatus` swaps Yellow/Blue; `createSumRGYB` is authoritative. `setSpriteStatus` treats its second argument as a **player number** (`iPower == currentPlayerNum` → own shading, since `45bb1e8`). With the literal `1`, **Player 2's heat shading showed Player 1's valuation** from this point on (confirmed at runtime 2026-11-02; history in §15). Owner-approved fix on `feat/ux-batch-4` (`75c54b0`): pass `j` (the local player, the same index used for `getMyVal`) in `Player.cs` and `PlayerAITutorial.cs`. The region status and all sums were never affected: `val = getMyVal(j)`.

Regions are color classes, not connected shapes. They can be empty (lines that do not cross inside the map, identical cuts) or fragmented by the map outline; all four buttons are always offered [Inferred]. The README's "overlap" special case is not implemented — `isIntuitive` (L364) always returns false.

---

## 4. The 1-2-1 selection sequence

Buttons call `Player.ChooseView(index)` ([L370](../Assets/Script/Player.cs#L370)). The precondition is `isAllTrue()` (both cuts received) and `0 ≤ index < 4`. Turn control uses the replicated counter `chooseNum`, advanced only by the `Choose` RPC.

| Guard (local) | Actor | Local effects | RPCs |
|---|---|---|---|
| P1 and `chooseNum < 1` | Player 1 | `choosePlayer[0] = sumRGYB[i]`; record slot 0 | `Choose(i,1)` All; note to P2 |
| P2 and `chooseNum == 1` | Player 2 | `choosePlayer[1] += sumRGYB[i]`; record slot 0 | `Choose(i,2)` All; note |
| P2 and `chooseNum == 2` | Player 2 | `choosePlayer[1] += sumRGYB[i]`; record slot 1 | `Choose(i,2)` All, then `GetValues` All; notes |

`Choose(i, p)` (L417, both clients): label "Taken By Player p", `Button.interactable = false`, `chooseNum++`. Clicking an already-taken button is impossible (non-interactable); any out-of-turn click is ignored. Guards run only on the clicking client; there is no host validation [Verified].

**Remainder** — `GetValues` (L445, both clients): `last = GetLast()` = index of the **first button that is still interactable** (L468). On the master, `sumRGYB[last]` is added to `choosePlayer[0]` and stored in record slot 1. Both clients then show the result (`Manager.setEndGameLayer`) and set `SumOfMyPart`.

---

## 5. Allocation percentages

All computed on each client for its own player only, in `float` [Verified]:

| Quantity | Formula | Code |
|---|---|---|
| Cut readout | `Green% = 100 · Σ_{Green cells} v_me / sumPlayer[me]`, `Red% = 100 − Green%` | `statusChange` (L531) |
| Region value | `region%[k] = 100 · Σ_{cells in k} v_me / sumPlayer[me]` | `setAllState` (L315), cached in `Manager.sumRGYB` |
| Final allocation | P1: `region%[pick] + region%[remainder]`; P2: `region%[pick1] + region%[pick2]` | `ChooseView`, `GetValues` → `choosePlayer[me]` |
| Opponent's region values (data only) | Same formula in the opponent's valuation, computed on the opponent's client and received via RPC | `SetForOtherPlayerColorValuesDataBase` |

No rounding is applied to stored values; UI shows the default float text. Each player's four region percentages sum to 100 up to float error. Envy-freeness, social welfare and similar metrics are not computed in-game.

---

## 6. Roles, host authority and state ownership

**Roles** [Verified] — `playerNum = PhotonNetwork.IsMasterClient ? 1 : 2`, re-evaluated at every use (18 occurrences in `Player.cs`). The role decides: which valuation (`myVal1`/`myVal2`), turn guards, who receives the remainder, line color, and labels. There is no stored or randomized role assignment.

**Host (master) authority** — limited to: sending `InitSquares`; deciding the next condition and triggering scene loads (`MasterNextGame`); drawing the payment round. The master does **not** validate cuts or choices.

**State ownership** (per client; nothing is held in Photon room properties):

| State | Owner | Replicated how |
|---|---|---|
| Cell index, totals `squares`, `sumPlayer[2]` | `Player` | Recomputed locally from scene data after `InitSquares` |
| Own cut (cell statuses during cutting) | `PointOfState.spriteStatus` | Sent once as `Cut` dictionary |
| Both cuts `player1`, `player2`, `playerCut[2]` | `Player` | `Cut` RPC |
| Region percentages (own valuation) | `Manager.sumRGYB` | Not replicated; computed locally |
| Turn counter `chooseNum` | `Player` | Incremented by `Choose` RPC on both clients |
| Which regions are taken | **`Button.interactable` on the 4 region buttons** | Set by `Choose` / `GetValues` |
| Own total `choosePlayer[me]` | `Player` | Not replicated |
| Cut-phase UI flags `isCut`, `isShowView` | `Manager` | Local |
| Next-round handshake `palyersGameOver[2]` | `Player` | `GameOverClicked` RPC |
| Round counter, condition | `Config` (DontDestroyOnLoad) | `RequestSceneLoad` arguments |
| Results list for payment | `RandArrays` (DontDestroyOnLoad) | Local |
| Payment index, winnings | `PlayerData` (DontDestroyOnLoad) | `SetRandomPaymentRound` RPC (index only) |
| Round record | `CutAndChoosePlayerDatabase` inside `Player` | Saved separately by each client |

Consistency between clients relies on both clients holding identical scene data, identical cell keys (§13.1), and receiving the same RPCs in order.

---

## 7. Photon communication and synchronization

All RPCs use the scene `PhotonView` on the `Player` object (line RPCs use `LineDrawingManager`'s view). `RpcTarget.All` executes locally at once and remotely via the server; reliable ordering per sender is assumed.

**One round, message sequence** [Verified order of calls; timing Inferred]:

```
Master (P1)                         Photon                         Client (P2)
Start: RPC InitSquares ─────────── AllBufferedViaServer ─────────► InitSquares
 (both) withSec: index cells, totals, shading; Cut button after ~1.2 s
P1 Cut: ReceiveSubmittedLine ──────────────────────────────────────► store line
        Cut(dict,1) ──── All ──────────────────────────────────────► store player1
                                                       P2 Cut: same, Cut(dict,2) ─► P1 stores player2
 (each client, when both cuts present)
   setAllState → regions, own %s; UpdateOtherPlayerName ↔; SetForOtherPlayerColorValuesDataBase ↔
P1 ChooseView: Choose(i,1) ─ All ──────────────────────────────────► chooseNum=1
                                         P2 ChooseView: Choose(j,2) ─► chooseNum=2 (both)
                                         P2 ChooseView: Choose(k,2), GetValues ─► both: remainder → P1
 (each) result panel; Next → SaveData; GameOverClicked / MasterNextGame → RequestSceneLoad(All)
```

| RPC | Declared in | Target | Notes |
|---|---|---|---|
| `InitSquares` | `Player` L141 | AllBufferedViaServer | Buffered — stays in the room cache across rounds |
| `ReceiveSubmittedLine` | `LineDrawingManager` L517 | Others | Coordinates only |
| `Cut` | `Player` L283 | All | `Dictionary<int,int>` keyed by sender's cell keys |
| `UpdateOtherPlayerName`, `SetForOtherPlayerColorValuesDataBase` | `Player` | Others | Data record only |
| `Choose`, `SetNoteForOtherPlayer`, `GetValues` | `Player` L417/L426/L445 | All / Others / All | Allocation |
| `sendMessageToAll` | `Player` L790 | Others | Chat |
| `NextGame` (button, local call), `GameOverClicked`, `MasterNextGame`, `RequestSceneLoad`, `SetRandomPaymentRound`, `LeaveGameForAll` | `Player` L827–924 | various | Progression (§8, §9) |

Not handled [Verified absence]: `OnPlayerLeftRoom`, `OnMasterClientSwitched`, `OnDisconnected`, reconnection, late join mid-round.

---

## 8. State machines: lobby to results

### 8.1 Session level

```
MainScene: Connecting ─OnConnectedToMaster→ JoinLobby ─OnJoinedLobby→ Lobby menu
  Lobby ─room button (name required)→ JoinOrCreateRoom("Room_k", 2 players)
        ├ creator: OnCreatedRoom → PhotonNetwork.LoadLevel("CutSceneAITutorial")
        └ joiner : AutomaticallySyncScene follows the master
CutSceneAITutorial (practice) ─both humans click Next→ RequestSceneLoad("CutScene", scenario 1, round 0)
CutScene round r ─both click Next→ CutScene round r+1      (r = 0…4)
CutScene round 5 ─both click Next→ SetRandomPaymentRound + LeaveGame("EndGameFlow")
EndGameFlow ─Finish→ PlayerData saved → survey redirect
Any game scene ─Home (either player)→ LeaveGameForAll → both return to MainScene (round not saved)
```

### 8.2 One real round (per client)

| State | Entry condition | Allowed actions | Exit → next state |
|---|---|---|---|
| **S0 Loading** | Scene loaded | — | `InitSquares` processed (+0.2 s) → S1 |
| **S1 Cutting** | Cells indexed; Cut button shown after 1 s | Draw/replace line; toggle see-other (Info conditions); chat (Chat conditions) | Cut pressed with 2 players → S2; with 1 player → S1 (line locked) |
| **S2 Waiting for opponent's cut** | `playerCut[me] = true`, `Manager.isCut = true` | Chat | Opponent's `Cut` arrives → S3 |
| **S3 Choosing** | `isAllTrue()`; regions computed; choose view shown | Region clicks gated by role and `chooseNum` ∈ {0,1,2} | `GetValues` received → S4 |
| **S4 Result** | Result panel and Next shown; result appended to `RandArrays` | Next; agreement toggle (chat layer) | Next → save record → S5 |
| **S5 Waiting for partner's Next** | `palyersGameOver[me] = true` | — | Both flags set on the second clicker → master's `RequestSceneLoad` → S0 of next round, or EndGameFlow after round 6 |

Within S3 the sub-states are `chooseNum` = 0 (P1's turn), 1 and 2 (P2's turns), 3→4 (remainder assigned by `GetValues`).

### 8.3 Practice scene differences

`CutSceneAITutorial` uses forks (`PlayerAITutorial`, `PointOfStateAITutorial`, `LineDrawingManagerAITutoria`, `SquareLineCalculatorAITutoria`, `SumSquaresAITutorial`) plus:
- `TutorialFplanManager`: starts when the auto-opened help page is first closed (`ShowAndHideOnClickAITutoria`); three configured steps (`Step0_GameIntro`, `Step1_CutIntroP1`, `Step1_CutIntroP2`; the step suffixed with the other role is skipped). While a step is shown, drawing and choosing are locked (`IsGameplayLocked`).
- `PCPlayerAITutorial`: after the human's cut, injects one of four hard-coded lines (uniform random) as the opponent's cut (`GenerateAndInjectLocalCut`), then makes uniform-random picks after 1 s (`MakePCChoices`).
- No allocation RPCs: choices run through `ExecuteLocalChoose`; when three regions are taken, `ExecuteLocalGetValues` gives the remainder to Player 1 after 0.5 s.
- Result shown with `setEndGameLayer(sum, false)` → not added to payment; no record saved.
- Exiting needs both humans' Next (same handshake as real rounds).

---

## 9. Round progression and experiment conditions

The schedule (table in [PROJECT_CONTEXT §6.3](PROJECT_CONTEXT.md#63-round-schedule)) is implemented entirely in `Player.MasterNextGame` ([L854](../Assets/Script/Player.cs#L854)), executed on the master after both players pressed Next [Verified]:

```
r = Config.addRoundNumber()           // counter after finishing round r−1
r == 2 → scenario = Random.Range(2,4)  // 2 Info or 3 Chat, reused for round 3 via getScenarioType()
r == 4 → scenario = 4                  // InfoAChat
r == 6 → SetRandomPaymentRound(Random.Range(0,6)) to All; LeaveGame("EndGameFlow")
else   → scenario = Config.getScenarioType() (unchanged)
RequestSceneLoad(sceneName, scenario, r) → every client: Config.createConfig(scenario, r); PhotonNetwork.LoadLevel
```

- `sceneName` comes from the Next button's serialized argument (`"CutScene"`), not from code.
- `Config.createConfig` (Config.cs L53) derives `chatEnable` (scenarios 3, 4) and `seeOtherEnable` (2, 4) and sets `timeForHelpPage = -1`.
- Conditions take effect in `Manager.Start` (chat layer and see-other button visibility) and in `Manager.changeSeeOtherPlayerBT` / `initialSeeOtherPlayerBT` (see-other only while `!isCut`).
- The round number shown to players is `roundNumber + 1`; the practice round also shows 1.

---

## 10. Persistence and result calculation

| Event | What happens | Code |
|---|---|---|
| Round start | Record initialized: round, condition name, start time, nickname, URL `userID`/`surveyID` | `Player.InitDatabaseAsync`, `SetDataFromURL` |
| Cut | Cut time, own cut %, per-cell cut | `CutView` |
| Both cuts | Own and opponent region %s, cell counts, opponent name | `Cut`, RPCs |
| Choices | Region indices received | `ChooseView`, `GetValues` |
| Result | `SumOfMyPart`; result appended to `RandArrays` | `GetValues`, `Manager.setEndGameLayer` |
| Next | `isAgreed` read from the toggle; record saved as `CutAChoose_{round}_{condition}` | `NextGame` → `SaveData` → `DatabaseManager.SaveData` (`ForceSaveAsync`, fire-and-forget) |
| After round 6 | Shared random index → each client: `winnings = 30 + max(0, (x − 45)·1.5)` with x = own `RandArrays[index]` | `SetRandomPaymentRound` → `PlayerData.SetRandIndex`, `SetNormalizeWinning` |
| Finish (EndGameFlow) | `PlayerData` saved; redirect with winnings | `EndGamePlayerDataManager.OnFinishButtonClicked`, `Config.RedirectToSurvey` |

Full field list: [PROJECT_CONTEXT §11](PROJECT_CONTEXT.md#11-data-collection-and-storage). Each client saves only its own record. The two records of a pair are linked only by nicknames (`PlayerName`/`otherPlayerName`), round and condition — there is no shared room/session ID [Verified].

---

## 11. Worked numerical example

Eight cells instead of 474; the arithmetic is identical. Assume the lines produce the sides shown.

| Cell | v₁ | v₂ | P1 side | P2 side | Region |
|---|---|---|---|---|---|
| c1 | 8 | 2 | Red | Red | Red |
| c2 | 8 | 4 | Green | Red | Blue |
| c3 | 2 | 8 | Red | Red | Red |
| c4 | 4 | 8 | Green | Green | Green |
| c5 | 2 | 2 | Red | Red | Red |
| c6 | 4 | 4 | Green | Red | Blue |
| c7 | 8 | 8 | Red | Green | Yellow |
| c8 | 4 | 4 | Green | Green | Green |
| Σ | 40 | 40 | | | |

1. **S1 Cutting:** P1's Red side = 8+2+2+8 = 20 → readout 50 / 50. P2's Red side = 2+4+8+2+4 = 20 → 50 / 50.
2. **S3 Region values** (`sumRGYB`, each client for itself):

   | Region (index) | P1 | P2 |
   |---|---|---|
   | Red (0) | 12/40 = 30% | 12/40 = 30% |
   | Green (1) | 8/40 = 20% | 12/40 = 30% |
   | Yellow (2) | 8/40 = 20% | 8/40 = 20% |
   | Blue (3) | 12/40 = 30% | 8/40 = 20% |

3. **Selection:** `chooseNum` 0 → P1 picks Red (30). 1 → P2 picks Green (30). 2 → P2 picks Yellow (20) and sends `GetValues`. `GetLast()` = Blue (index 3, the only interactable button) → P1 +30.
4. **S4 Result:** P1 = 60%, P2 = 50%. Cross-check (not computed in-game): P2 values P1's bundle at 30 + 20 = 50%; P1 values P2's bundle at 20 + 20 = 40% → no envy.
5. **Records:** P1 `PlayerColorValues {RedX:30, GreenO:20, RedO:20, GreenX:30}`, `OtherPlayerColorValues {30, 30, 20, 20}`, `AmountOfColor {3, 2, 1, 2}`, `PlayerChoosenColors [0, 3]`, `SumOfMyPart 60`. P2: `PlayerChoosenColors [1, 2]`, `SumOfMyPart 50`.
6. **Payment if this round is drawn:** P1 = 30 + (60 − 45)·1.5 = 52.5; P2 = 30 + (50 − 45)·1.5 = 37.5.

**Protocol property behind the in-game "50%" hint** (derived, not computed in code): with an exact 50/50 own cut, P1 secures ≥ 50% by taking its most valuable region (the remaining region of the opposite half is worth at least 50 − max), and P2 secures ≥ 50% by taking the two regions of whichever own half P1 did not touch. In this example both cuts were exactly 50/50 and both players reached ≥ 50%. On the real map an exact split may not exist (smallest step = 2/1976 ≈ 0.10% for P1).

---

## 12. Dependency map

```
             ┌──────────── Launcher (MainScene) ────────────┐
             │ Photon connect/lobby/rooms; Config.createConfig; PlayerData.SetPlayerData
             ▼
 Config ◄──────────── Player (CutScene) ─────────────► PhotonNetwork / PhotonView RPCs
 (round, condition,      │  rules, scoring, RPCs, chat,       ▲
  survey redirect)       │  record, schedule, scene loads     │ ReceiveSubmittedLine
   ▲                     │                                    │
   │      ┌──────────────┼───────────────┬──────────────┐     │
   │      ▼              ▼               ▼              ▼     │
   │   Manager       PointOfState    SumSquares   LineDrawingManager ──► SquareLineCalculator
   │  (UI state,     (cells, values, (county       (input, submit,         (classification;
   │   sumRGYB,       status, keys)   titles)       line reveal)            calls Player.statusChange)
   │   see-other,        ▲                                 │
   │   end panel)───┐    └──── PointOfState.OnMouse* → Player.inst.statusChange
   │                ▼
   │            RandArrays ──► PlayerData ──► EndGamePlayerDataManager ──► Config.RedirectToSurvey
   │                               │
   └───── Manager.Start reads Config      CutAndChoosePlayerDatabase ──► DatabaseManager ──► UGS Cloud Save
                                            (owned by Player)                 (anonymous auth)

 Practice: PlayerAITutorial ── PCPlayerAITutorial, TutorialFplanManager,
           PointOfStateAITutorial, LineDrawingManagerAITutoria, SquareLineCalculatorAITutoria,
           SumSquaresAITutorial, and the SAME Manager, Config, RandArrays, PlayerData.
 Scene UI buttons → Player.ChooseView/CutView/NextGame/getOtherView/sendMessage2, LoadScene (Home)
```

| Relationship | Mechanism | Evidence |
|---|---|---|
| Player ↔ Manager | Static singletons both ways (`Manager.inst` from Player; `Player.inst` from cells and line code) | `Player.cs`, `Manager.cs`, `PointOfState.cs`, `SquareLineCalculator.cs` |
| Player → line drawing | `GameObject.Find("LineDrawingManager")` | `CutView`, `setAllState` |
| Cells → Player | `Player.inst.statusChange()` in mouse handlers | `PointOfState.OnMouseEnter/OnMouseDown` (no-op while `Manager.status == 0`, which is always) |
| Line drawing → cells | `FindGameObjectsWithTag("SquarePoint")` | `ColorAllSquaresByLine` |
| UI → logic | Scene-serialized `OnClick` bindings with literal arguments (region index, `"CutScene"`, `"MainScene"`) | `CutScene.unity` |
| Round management → scenes | Strings `"CutScene"` (button arg), `"EndGameFlow"`, `"MainScene"` (code/args) | `Player.MasterNextGame`, `LoadScene` |
| Scoring → payment | `Manager.setEndGameLayer` appends to `RandArrays` | `Manager.cs` L126 |
| Practice ↔ core | Copy-and-rename forks; shared Manager/Config | §13.2 |

---

## 13. High-risk coupling points

### 13.1 Cell ID drift (allocation consistency)

- **Mechanism** [Verified]: `PointOfState.myKey = key++` in `Start`, with `key` a static field that is never reset; in a player build, static state persists across scene loads for the whole browser session.
- **Practice offset** [Verified scene data]: in `CutSceneAITutorial.unity`, the 71 Tyrone cells have **both** `PointOfState` and `PointOfStateAITutorial`. Their `PointOfState.Start` consumes keys 0–70 during practice.
- **Consequence** [Inferred]: in real round r (0-based) cell keys are approximately `71 + 474·r … 71 + 474·r + 473`, provided the client ran practice once and `Start` order is the same. `Unknown`: Unity does not document `Start` order as deterministic across clients.
- **Runtime observation (2026-11-01, Editor + WebGL Development build):** after practice, round 1 keys on the Editor client were `0…473` (the +71 offset did **not** occur), and both clients showed identical region maps. So the drift did not appear in this run; the mechanism above remains a risk for any change to scene composition or load order.
- **Why it matters**: the `Cut` RPC sends the sender's key → status map, and `createSumRGYB` looks up `player1[myKey]` and `player2[myKey]` using the receiver's keys. Different offsets → `KeyNotFoundException` (round cannot proceed); same range but different order → **silently different regions on each client**, so each player's percentages refer to a different allocation. Nothing detects this.
- **Data impact**: `playerCutSquares` keys are not stable cell IDs across rounds, sessions or players. County + status per key remain usable (counties are value-uniform), cell geometry is not recoverable.
- **Triggers to avoid**: adding/removing/reordering `PointOfState` objects in any scene a client loads before or during the rounds, skipping or repeating practice for one player, adding scenes with `PointOfState`, reloading a round on one client only.

### 13.2 Duplicated scoring logic

| Logic | Real rounds | Practice | Legacy |
|---|---|---|---|
| Region classification + totals | `Player.createSumRGYB` L484, `setAllState` L315 | `PlayerAITutorial.createSumRGYB` L632, `CalculateSumRGYBForPlayer` L356, `GetSumRGYBForPlayer` L345 | `PlayerVsAI.createSumRGYB` L227 |
| Cut readout | `Player.statusChange` L531 | `PlayerAITutorial.statusChange` L679 | `PlayerVsAI` L282 |
| Opponent view | `Player.getOtherView` / `setOtherCutMap` | `PlayerAITutorial.getOtherView` L719 / `setOtherCutMap` L831 | — |
| Side classification | `SquareLineCalculator` | `SquareLineCalculatorAITutoria` (identical except type names) | — |
| Cell component | `PointOfState` | `PointOfStateAITutorial` (identical except `Player.inst` references; separate static `key`) | `PointOfStateAI` |
| Remainder rule | `GetValues` via RPC | `ExecuteLocalGetValues` (P1 hard-coded as index 0) | — |

`Player.cs` vs `PlayerAITutorial.cs` differ in 405 lines (whitespace ignored). A rule, text or formula change made in one place will make practice and real rounds diverge unless mirrored. Valuations are also duplicated across the two scenes.

### 13.3 UI-dependent game state

- Allocation: `GetLast()` (real) and `GetAvailableChoiceIndices` / `GetChosenCount` (practice, PC picks, auto-remainder at 3 taken) read `Button.interactable` of the four region buttons. Making a button non-interactable for UX reasons changes who gets which region.
- Scoring cache in a UI class: `Manager.sumRGYB` is read for scoring by `ChooseView`/`GetValues`.
- Payment capture inside a UI method: `Manager.setEndGameLayer(sum, isReelGame)` appends to `RandArrays`; calling it twice, or with the wrong flag, changes payments.
- Shared labels: `buttonsValueText[0..1]` serve as cut readouts and as region labels.
- Condition enforcement in UI: the Info-condition timing lives in `Manager.changeSeeOtherPlayerBT` / `initialSeeOtherPlayerBT` (`!isCut`).
- Scene-serialized bindings: region indices 0–3 and the next-scene name are literal `OnClick` arguments in `CutScene.unity`; stale bindings exist (`Manager.setStatusClick`, which no longer exists, on inactive `RedBT`/`GreenBT`/`IdleBT`; `CutBT` typed as `myName`) — resolution at runtime is [Unknown].

### 13.4 Other coupling points

- **Role derivation** from `IsMasterClient` everywhere; a master switch flips roles mid-round.
- **Next handshake** (`NextGame` / `GameOverClicked`): simultaneous clicks can leave both flags set with nobody sending `MasterNextGame` [Inferred]; save happens inside the same method.
- **Name/tag lookups**: `"LineDrawingManager"`, `SquarePoint`, tags `Config` / `DatabaseManager`.
- **Session singletons** (`Config`, `RandArrays`, `PlayerData`) persist across `MainScene` visits; `RandArrays` is never cleared.

---

## 14. Research invariants

### 14.1 Verified requirements (owner-stated and matched by code)

| Area | Must not change accidentally | Where enforced |
|---|---|---|
| Selection rules | P1 one region → P2 two regions → P1 the remainder; exactly four candidate regions from two independent cuts | `ChooseView`, `Choose`, `GetValues`, `createSumRGYB` |
| Subjective valuations | Each player's percentages use only that player's own cell values and own total | `statusChange`, `setAllState`, `choosePlayer` |
| Independence of cuts | Opponent's cut and line hidden until both submitted | `Cut`, `ReceiveSubmittedLine`, `ShowBothLines` |
| Result calculation | Result = sum of own percentages of received regions | `ChooseView`, `GetValues` |

### 14.2 Code-confirmed behavior that research depends on (freeze; changes need owner approval)

| Area | Current behavior |
|---|---|
| Valuations / map | 474 cells, per-county values in §1, totals 1976 / 2152, same map every round; copies in `CutScene` and `CutSceneAITutorial` must match |
| Experimental conditions | `Normal`, `Info`, `Chat`, `InfoAChat`; own valuations always visible; opponent valuations only in Info conditions and only before own cut; chat only in Chat conditions |
| Round progression | Practice → rounds 1–2 Normal → 3–4 one random draw of Info/Chat → 5–6 InfoAChat → EndGameFlow |
| Multiplayer allocation consistency | Both clients must derive identical regions and allocation from identical scene data and keys; master = Player 1 |
| Data collection | Record fields and key format `CutAChoose_{round}_{condition}`; region key names `RedX, GreenO, RedO, GreenX`; save on Next; practice unsaved |
| Reward | Uniform draw over the 6 real rounds, same index for both; `30 + max(0, (x − 45)·1.5)`; practice excluded |
| Participant texts | Bilingual instructions incl. the 50% hint |

### 14.3 Open questions requiring owner decision

1. Fixed Player 1 role and valuation profile per participant vs. counterbalancing.
2. Opponent valuations hidden during the choose phase in Info conditions — intended?
3. Allow submission without a drawn line, and empty regions? (UX-004: current behaviour and the two smallest corrections are documented in §2; nothing changed in Batch 4.)
4. Identical map and valuations in all six rounds — intended?
5. Payment formula and currency confirmation.
6. Required behavior on disconnect or Home (currently: session ends for both, round unsaved).
7. Player 2's heat shading during choosing shows Player 1's valuation (§15). The correction was approved for Batch 4 and is implemented on `feat/ux-batch-4` (`75c54b0`, not yet integrated). Still open: how to treat data collected with builds that include `45bb1e8`.

---

## 15. Edge cases and failure scenarios

| Scenario | Behavior | Tag |
|---|---|---|
| Opponent disconnects | No handler; remaining player waits; if master left, the other client becomes "Player 1" | Verified absence of handlers; outcome Inferred |
| Either player presses Home | Both return to `MainScene`; round unsaved | Verified |
| Simultaneous Next clicks | Possible permanent wait | Inferred |
| Late join to practice after partner pressed Next | Missed `GameOverClicked` → permanent wait | Inferred |
| Player never presses Next | Round record never saved | Verified |
| Cloud Save initialization or save fails | Logged only; data lost | Verified |
| Submit without a line / zero-length line | 100/0 cut (or the colours of an `Esc`-cancelled redraw); no line shown for that player; drawing still possible after submit; Player 1's no-line cut leaves Green and Blue empty (details §2, UX-004) | Observed 2026-11-03 in test mode (practice, Player 1 role, Editor and WebGL); Player 2 case and Esc variant Inferred |
| Help Page open over drawn lines | Before the Batch 4 follow-up, the Batch 4 outlines (outside `States`) stayed visible over the Help Page. Fixed in `349f6e2` | Verified 2026-11-04 (practice and round 1, WebGL and Editor; restored after closing) |
| Press on UI over the map | Before Batch 4, a press on an open Help Page or on the part of `SeeOtherPlayerBT` that overlaps the drawable bounds started a line. Fixed on `feat/ux-batch-4` (UX-026); county labels still start a line | Fix verified 2026-11-03 (WebGL 960×600: Help Page, see-other edge at world (−0.90, 4.26), Tyrone label) |
| Empty or fragmented regions | Selectable, 0% if empty | Inferred |
| Cell keys differ between clients | Exception or silently inconsistent allocation (§13.1) | Inferred (not observed 2026-11-01) |
| Second player to cut | Before Batch 3: after both cuts, saw "wait for the other player to cut" instead of the turn note (`CutView` set that note after `setAllState`). Fixed in Batch 3 (`dev` `8b29582`): the note is shown only while the other cut is missing | Observed 2026-11-01; fix verified in test mode (both cut orders) |
| End of choosing | Before Batch 3: the result breakdown was overwritten by "…last remaining part" notes. Fixed in Batch 3: `SetNoteForOtherPlayer` ignores notes once `chooseNum >= 3` | Fix verified in test mode on both clients |
| Player 2 heat shading after both cuts | Switches from Player 2's to **Player 1's** valuation (overlay colour) for the choose phase and result, in Normal, Info and practice; Player 1 unaffected; county titles, region %, scores, records and payment still use Player 2's own values. Cause: `createSumRGYB` → `setSpriteStatus(status, 1)` with player-number semantics (`45bb1e8`, 2026-07-31). Before `45bb1e8` the literal `1` meant "own", but Player 2's Info "see other" view would then have shown its own shading [Inferred from history]. Records saved with builds containing `45bb1e8` and not `75c54b0` were collected while Player 2 saw Player 1's shading during choosing; their numbers are correct, but the information shown to Player 2 differed from the intended protocol. **Fix (owner-approved): `feat/ux-batch-4` `75c54b0`, `setSpriteStatus(status, j)`** | Defect confirmed 2026-11-02. Fix verified 2026-11-03 in test mode: Editor as Player 2, overlay of all 375 distinguishing cells matches Player 2's valuation before and after both cuts (practice and rounds 1–5; 0 match Player 1). Info pre-cut view still shows Player 1's valuation (375/375) and restores. Region values on both clients equal an independent recomputation |
| Repeat play without page reload | Stale `RandArrays` used for payment | Inferred |
| Help Page closed after the cut (Info rounds) | Before Batch 2: `SeeOtherPlayerBT` reappeared; one click showed the opponent's heat map and region values during choosing and added to `CountPlayerSeeOther`. Fixed in Batch 2 (`dev` `0f3f99d`): hidden layers are restored to their pre-open state and `getOtherView` is a no-op after the own cut | Observed 2026-11-02; fix verified in test mode |
