# UX Clarity Batch — Visual Changes for Research Review

> Branch `feat/ux-clarity-batch` (from `dev` `34b3141`), 2026-11-14. Scope: the four owner-approved presentation improvements from the work plan *fair-division-2d-ux-improvements-workplan* (heatmap clarity, selection feedback, cut-line visibility, Info-mode clarity) and the **investigation-only** item on the 50 % explanation (§6, **PENDING RESEARCH APPROVAL**).
>
> **Status (2026-11-16):** merged into `dev` (PR #11, `d595f3c`), followed by the owner-approved **visual polish** (PR #12, `dc28f92`): dark-red opponent-map mode, smooth region contours and the readout-button treatment. The descriptions below are updated to the merged look; evidence of the polish: [visual-polish-evidence/](visual-polish-evidence/README.md) (Editor) and [webgl-safety-evidence/](webgl-safety-evidence/README.md) (WebGL and two clients, §7).
>
> These are presentation changes, not a new experiment version: no rule, valuation, condition, schedule, RPC, payload, saved field, reward or participant-facing string changed. Because what participants see can still influence behaviour, the research owners should review §2 before this version is used with real participants. Evidence tags as in [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md): **[Verified]**, **[Inferred]**.

## 1. Summary for the research team

| # | What the participant sees now | Where | Why it is presentation only |
|---|---|---|---|
| 1 | **Heatmap:** the inner square of each map cell goes from dark (low value) through grey (medium) to near white (high value). Before, the three levels differed only in a faint red tint (brightness ≈ 0.39 / 0.50 / 0.61). The existing low / medium / high legend under the map is redrawn with the same colours. | Real rounds and practice; own and Info (other-player) views | Same three levels for the same cells (values 2 / 4 / 8), same order, same per-player scale (`value / own total × 500`, unchanged code). Only the colour of each level changed. "Lighter = more valuable" (the Help page sentence) still holds — more clearly. No numbers added. |
| 2 | **Region cards ↔ map:** pointing at (or keyboard-focusing) a region card dims the other regions on the map and outlines the pointed one with **one smooth contour** just outside its cells (anti-aliased line over a soft halo; since PR #12 — before: stair-step edges per cell side). After a choice, regions you received keep a smooth **amber contour** (same amber as the card's "chosen by you" check badge); regions the other player received keep a **dark veil** (like the card's muted "lock" state). Veils and contours fade in over 0.14 s. | Real rounds and practice, choose phase and result | Driven by the same notifications that already update the cards (`Choose`, `GetValues`, phase instruction). A region is marked on the map exactly when its card already shows who took it — choices in this protocol are public the moment they are made (the `Choose` RPC goes to both players and both cards update immediately). Region colours, values, selection order, button mapping and ownership are untouched. |
| 3 | **Cut lines:** a finished cut line is drawn from map edge to map edge. A short drag no longer looks like a short cut while the colours split the whole map. | Real rounds (own and other player's line after both cuts) and practice (own and computer line) | The game has always divided the map by the infinite straight line through the two drawn points; the extension shows exactly that line. Drawn points, classification, submitted coordinates and saved cut data are unchanged (§3). |
| 4 | **Info mode (opponent-map mode, since PR #12):** while a player is viewing the other player's values, the screen switches to one dark-red accent with a 0.17 s fade: deep-red frame bars with a soft outer glow around the map, a neutral dim of the board behind the cells (heat squares, cell colours and labels are not tinted), the see-other toggle in deep red with a light ring and one soft pulse, the value-owner plate ("מוצגים הערכים של השחקן השני") in deep red, legend and map titles in soft rose, and the red/green side readouts get a thin light border each and an opaque dark-red card behind the pair — their red/green fills, texts (the other player's 5-decimal values) and clickability are unchanged. Returning to the own view restores every colour exactly. (PR #11 had a plain red frame and a white toggle label.) | Real rounds in `Info` / `InfoAChat`, before the own cut (the only time the toggle exists) | Same state that already switched the heading plate ("מוצגים הערכים של השחקן השני"), the legend title and the button colour. No new text, no change to when the view is allowed, what it shows, or `CountPlayerSeeOther`. Practice has no Info view, so nothing changed there. |

## 2. Points the research owners may want to confirm

1. **Heatmap appearance.** Participants in earlier sessions saw the faint red-tint heatmap; new sessions see a clearer lightness scale. The information is identical, but value differences are now easier to notice. If data from both versions will be pooled, record the build/commit per session (PROJECT_CONTEXT §13.3 item 5).
2. **Opponent's regions dimmed after selection.** After the other player chooses, their regions are veiled (35–40 % dark) on your map. Heat values stay visible underneath. This mirrors the existing muted card; if the research team prefers no dimming of the opponent's regions, the veil strength is one Inspector value (`RegionSelectionView.otherVeil`, alpha 0 = off).
3. **Spotlight while hovering.** Hovering a card dims the other three regions temporarily (45 % since PR #12; 55 % before). It is shown in every turn (including while the other player chooses) because it only repeats which cells already have that region colour.
4. **The full cut line** makes it visible that a short or off-centre drag divides the whole map. This is what the rules already did; it may reduce accidental cuts, which is the intent of the improvement.
5. **Opponent-map colour.** Dark red (approved with PR #12) matches the existing red toggle and the dark-red "other player's values" plate; it marks the whole screen state, not only the map, so participants are less likely to cut while reading the other player's values (the Cut button is hidden in that state anyway, unchanged).

## 3. Cut geometry — why the extension is the same cut

- **Calculation** (unchanged): `SquareLineCalculator.GetSquareColorByLine(cell, start, end)` orients the two points left-to-right and uses the 2D cross product — every cell is classified by its side of the **infinite** line through `start` and `end` [Verified]. The practice computer's cuts use the same function on their own two points.
- **Display before:** the `LineRenderer` showed only the dragged segment.
- **Display now:** a second, display-only `LineRenderer` per line (under the outline holder, beside the line manager) draws the same line clipped to the visible map box (`MapBoundaries` bounds), joined with the drawn segment. The original renderer, `startPoint` / `endPoint`, the 50 ms recolouring, `SubmitLine` / `ReceiveSubmittedLine` coordinates, `Cut` dictionaries and records are untouched.
- **Check [Verified, Editor, both scenes]:** 17 test cuts — horizontal, vertical (both directions), two diagonals, near-horizontal, near-vertical, 0.05 and 0.01-unit drags, near the top-left and bottom-right corners, starting in the drawable strip outside the box, full length, and the four practice computer cuts. For every cut, the shown line lies on the cut line (max offset 2·10⁻⁷), contains the drawn segment, ends on the box border, and **0 of 474 cells** change side or status between the drawn segment and the shown line.
- **Not extended (on purpose):** the own line while it is still being dragged, and any own line whose shown points differ from the points the cells were coloured with. The second case exists before this batch: a press released before the first drag update shows a zero-length line, but the cells (and, after Cut, the sent coordinates) use the previous `endPoint`. Extending that would draw a line the player never drew, so it is left as it was and reported in §5.

## 4. Implementation map

| Item | Files | Scene objects |
|---|---|---|
| Selection ↔ map | `UI/GameFeel/RegionMapHighlight.cs` (new; smooth contours since PR #12), `UI/GameFeel/UiGeneratedSprites.cs` (PR #12, runtime textures), `RegionSelectionView.cs`, `RegionCard.cs`, `RegionCardPointer.cs` | `RegionSelectionView.mapRoot` = `Canvas/States` (both scenes); runtime holder `Canvas/States/RegionMapHighlight` |
| Heatmap | `UI/GameFeel/HeatShading.cs` (new), `UI/GameFeel/HeatLegendView.cs` (new), `PointOfState.cs`, `AITutorial/PointOfStateAITutorial.cs` | `HeatLegendView` on `Canvas/ViewItems/SquareValueExplane` (both scenes) |
| Info mode | `Manager.cs` (delegates the indicators), `UI/GameFeel/OpponentMapMode.cs` (PR #12) | `Canvas/States/OtherMapFrame` (CutScene, inactive by default), `Manager.otherMapFrame`, `Manager.seeOtherLabelRTL` |
| Cut line | `RefactoredUI/LineDrawingManager.cs`, `AITutorial/LineDrawingManagerAITutoria.cs` | `lineExtentBox` = `Canvas/States/MapBoundaries` (both scenes); runtime `…Outlines/<line>Extension` |

All new runtime objects are display-only: untagged, no `PointOfState`, not under a county, not under the line manager (so `FindGameObjectsWithTag("SquarePoint")`, county sums, `IncludeMapCellsHiddenByHelp` and the line-owner label are unaffected), hidden with the map when the Help page opens, and destroyed with the scene. No `.meta` file was edited by hand; Unity generated the three new script GUIDs.

## 5. Pre-existing findings (not changed)

1. **Click without drag keeps an old end point** (`LineDrawingManager`, both copies): `StartDrawing` resets the shown line to one point but not `endPoint`; if the button is released before the first `UpdateDrawing`, `FinishDrawing` colours the cells by the line through the click point and the previous `endPoint` (or `(0,0,0)` in a fresh round), and `SubmitLine` sends those points, so the other player sees a line the drawer never saw. Rare with a real drag. Not extended by this batch; a fix (set `endPoint = startPoint` in `StartDrawing`) would change which cut such a click produces, so it needs an owner decision (it is related to the UX-004 no-line rule).
2. **See-other toggle label** keeps the text "הצגת מפת החום של השחקן השני" while the other map is shown. A second label (e.g. "חזרה למפה שלכם") would be clearer but is new participant text — proposal only, needs research approval. *Resolved 2026-11-27 (approved wording, `feat/ux-help-tooltip-batch`): "הצג את ערכי השחקן השני" / "חזרה לערכים שלי" (PROJECT_CONTEXT §17).*
3. County labels near the top map edge ("Antrim =", "Derry =") are crossed by cut lines that reach the edge (also before this batch for long drags).
4. **Help-page illustrations:** both Help pages show two legacy PNGs labelled "Low" / "High" (old cell colours: dark teal vs. tan). After the heatmap change they no longer match the map's colours, although the direction (lighter = higher) is the same. They are Help content (class A), so they were not changed; redrawing them in the new colours (same labels, or Hebrew labels) needs research approval. *Resolved 2026-11-27 (approved): the Help legend is a copy of the live map legend (Hebrew labels, three levels, drawn by `HeatLegendView`) and the old "Belfast Map" picture is replaced by a neutral render of the current map (PROJECT_CONTEXT §17).*

## 7. Verification summary and evidence

Test mode only (Photon `uxtest_2.42`, Unity Services `development` / `uxtest`, TEST MODE badge), no production connection. Details: [PROJECT_CONTEXT.md §17 "UX clarity batch"](PROJECT_CONTEXT.md#17-current-status-and-handoff); images: [ux-clarity-evidence/](ux-clarity-evidence/README.md).

| Check | Result |
|---|---|
| Compile, scene integrity | 0 errors; scene diffs additive (plus two re-serialized TMP caches); no `OnClick` / interactable / text / tag change on existing objects |
| Cut geometry (17 cuts × 474 cells, both scenes) | 0 cells change side between drawn segment and shown line |
| Region values, two clients, six rounds | Independent recomputation from the transmitted cuts = game values (max diff 0) and = the other client's display |
| Opponent selection timing | No mark before a choice; the other player's region marked exactly when its card shows it (both roles) |
| Info / InfoAChat | Frame and label only while viewing the other map, restored on return, gone after the own cut; `CountPlayerSeeOther` 0 → 1 → 2 as before |
| Progression and data | Practice → 6 rounds (Normal, Normal, Info, Info, InfoAChat, InfoAChat) → end screen; 7 records per player confirmed, completion gate PASSED; read-back: 25 round fields unchanged; payment 30 + max(0, (x − 45)·1.5) on the shared index |
| Visual polish (PR #12), 2026-11-16: WebGL Development build + Editor, two clients, test mode | Opponent-map mode renders in WebGL; readouts in that mode = the other player's values from Editor state (62.45353 / 37.54647 %); own → opponent → own pixel-identical; `CountPlayerSeeOther` 0 → 1 → 2; toggle gone after the own cut; smooth hover and ownership contours in practice and in an InfoAChat round; region values identical on both clients; Info / Chat / Normal UI permissions (Editor, Photon offline mode). Details: [PROJECT_CONTEXT.md §17 "WebGL Development build safety"](PROJECT_CONTEXT.md#17-current-status-and-handoff) |
| Not verified | Release build, browser fullscreen, touch, runtime keyboard navigation, low-end frame rate; the polish with a GPU browser at full frame rate (the 2026-11-16 WebGL run used software rendering) |

## 6. Item 5 — The 50 % explanation — **PENDING RESEARCH APPROVAL**

Investigation only. **Nothing was changed** (wording, timing, practice instructions, Help page). The decision belongs to Rika / the lecturers.

### 6.1 Exact current wording and where it appears [Verified in code and scene data]

| # | Text (as displayed) | Where / when | Practice |
|---|---|---|---|
| a | **"אנא חלקו את המפה לשני חלקים (אם תחלקו לשני חלקים שווים, מובטח שתקבלו לפחות 50%)"** + "אפשר לשרטט מחדש עד שתלחצו על אישור החלוקה." (English legacy text: *"Please divide the map into 2 parts (if you divide into 2 equal parts you are guaranteed to get at least 50%)"*) | Note card next to the map, written in `Player.Start` (`Player.cs` line 99) at the start of **every** round, and visible for the whole cut phase until the player's own cut (then cleared) | Same sentence (`PlayerAITutorial.cs` line 94) |
| b | **"המפה מחולקת ל־50%"** + "(אם תרצו, תוכלו לשנות את החלוקה או לבחור שלא לחלק באופן שווה)" | Replaces the note whenever the live split readout is exactly 50 % (`Player.statusChange`, float equality — rarely reached, see 6.2) | Same (`PlayerAITutorial.statusChange`) |
| c | Help page bullets: **"אם תחצו את המפה לשני חלקים שווים בדיוק בערכם (50-50), מובטח לכם לקבל לפחות 50% מהשווי."** and "אם תחצו לחלקים לא שווים, תוכלו לזכות ביותר מ-50% או להפסיד ולקבל פחות – תלוי בבחירה של השחקן השני." | `CutScene` Help page; opens automatically about 1 s after each round starts (`HelpPageCountdown`) and on the "?" button | Practice Help page (own copy of the text) |
| d | Tutorial Step 0: **"חלוקה 50/50 לפי הערכים שלכם מבטיחה לכם לפחות 50%."** | Practice only, first explanation card, after the Help page is closed | Practice only |

The participant therefore meets the guarantee in up to four places, and (a) is repeated as a persistent note in all seven rounds.

### 6.2 What "50 %" means in this game

- It is a share of the participant's **own subjective valuation** (sum of their own cell values, totals 1976 for Player 1 and 2152 for Player 2), not of area. The value caption already says "האחוזים מציינים את שווי השטח עבורכם - לא את גודלו". Sentence (a) says only "שני חלקים שווים" (two equal parts) — it does not say "by your values", so it can be read as equal **area**; (c) and (d) do say "by value".
- The property behind it [derived, not computed in code — reference §11]: if a player's own cut is exactly 50/50 in their own values, they can still secure ≥ 50 % — **but only with the matching choice**: Player 1 must take their most valuable region (the remainder they receive is then worth at least 50 − max); Player 2 must take both regions of the own half that Player 1 did not touch. The sentences present the guarantee as a consequence of the cut alone.
- On this map an exact 50/50 split usually cannot be drawn (value steps of 2/1976 ≈ 0.1 % for Player 1; the readout shows values like 44.02634 %), so (b) rarely appears, and "at least 50 %" means "about 50 %" in practice.

### 6.3 Intended instructional role

It explains the protocol's safety (proportionality) guarantee — the standard reason a "cut in half" is a reasonable baseline in cut-and-choose — so that participants understand they are not exposed to an arbitrarily bad outcome. It is classified **A (research-critical)** in [MICROCOPY_INVENTORY.md](MICROCOPY_INVENTORY.md) and listed under participant texts in the research invariants (CUT_AND_CHOOSE_REFERENCE §14.2).

### 6.4 Possible anchoring concerns

- **Default-strategy nudge:** the sentence names one cut (equal halves) and attaches a guarantee to it. Participants may treat it as the recommended strategy, which can reduce the variance of cuts and the amount of strategic cutting the study wants to observe.
- **Repetition:** (a) is shown during every cut phase, plus the Help page each round; repeated exposure strengthens the anchor more than a one-time explanation.
- **Interaction with conditions:** in `Info` / `InfoAChat` rounds, seeing the other player's values can motivate a deliberately unequal cut; a persistent 50/50 cue works against exactly the behaviour these conditions may elicit, which can shrink measured condition effects.
- **Feedback loop:** (b) rewards hitting exactly 50 % with a confirmation message (although it adds "you may change it").
- **Incompleteness / ambiguity:** (a) omits "by your values" and the choice-step condition, so it can be over-trusted (an equal-area cut is not safe) or misunderstood.
- Counterpoint: removing it changes what participants know about the protocol; participants who do not discover the guarantee may cut more randomly, and data would no longer be comparable with sessions that showed it.

### 6.5 Options

| Option | What changes | For | Against |
|---|---|---|---|
| **A. Keep the current wording** | Nothing | Comparable with all data collected so far; participants know the safety property | Keeps the default-strategy nudge in every round; (a) stays ambiguous (area vs value) and incomplete (choice step) |
| **B. Remove the explanation** | Remove (a)'s guarantee clause, (b), the Help bullets and Step 0 sentence | No anchoring from the instructions; cuts reflect participants' own reasoning | Participants lose a key property of the protocol; more confusion / risk aversion possible; new data not comparable with earlier sessions; a protocol change to document |
| **C. Neutral wording** | Keep one factual explanation (Help page / practice), remove the guarantee from the persistent per-round note and the exact-50 % message, and phrase it as a property rather than advice, e.g. *(draft direction only, not approved text)*: "החלק שתקבלו נמדד לפי הערכים שלכם. בשיטה זו כל שחקן יכול להבטיח לעצמו לפחות מחצית מהשווי לפי הערכים שלו, אם יחלק לשני חלקים שווי ערך עבורו ויבחר בהתאם; חלוקות אחרות עשויות להניב יותר או פחות." | Participants still understand the protocol; much weaker nudge; fixes the area/value ambiguity and the missing choice condition | Wording must be approved and back-translated; still a (smaller) change relative to earlier sessions |

### 6.6 Recommendation (for discussion)

**C, applied consistently** — state the guarantee once, accurately ("by your own values", with the choice condition), in the Help page and practice, and remove it from the note that stays on screen in every round and from the exact-50 % confirmation. This keeps the protocol understandable while removing the repeated recommendation. If data has already been collected with the current wording and pooling across sessions matters more than reducing the nudge, **A** (with the session/build recorded) is the conservative alternative. B is not recommended without a pilot, because it removes information the participants may need.

**Decision record:** ☐ A ☐ B ☐ C — *PENDING RESEARCH APPROVAL (Rika / lecturers). No change may be made before an explicit written decision; any approved change must be applied to all four places in both scenes and both C# copies, then re-verified (texts identical across screens, rules unchanged).*

**Decision (2026-11-27, research supervisors, relayed by the project owner):** replace the prominent 50 % explanation with an **optional information tooltip** — a variant of C. Implemented on `feat/ux-help-tooltip-batch` in both scenes and both C# copies:

| Place (§6.1) | Now |
|---|---|
| a — per-round cut note | Guarantee clause removed; the note keeps "אנא חלקו את המפה לשני חלקים." + "אפשר לשרטט מחדש עד שתלחצו על אישור החלוקה." The explanation is in an "i" icon next to the cut instruction (`CutStatusPlate`), shown on hover only during the player's own cut phase: **"למה כדאי לחלק 50/50?"** / **"אם תחלקו את המפה לשני חלקים שערכם עבורכם 50% כל אחד, תוכלו להבטיח לעצמכם לפחות 50% באמצעות בחירה מתאימה. בחלוקה אחרת תוכלו לקבל יותר מ־50%, אבל גם פחות."** (states "by your values" and the matching-choice condition — the two gaps noted in §6.2) |
| b — exact-50 % note | Removed (`statusChange`; the returned value and readouts are unchanged) |
| c — Help page bullets | Unchanged (the Help page now opens only from "?") |
| d — practice Step 0 | Unchanged |

Data recorded with this version is not directly comparable with earlier sessions on cut behaviour (and on start-to-cut time, since the Help page no longer opens at each round start); record the build/commit per session (PROJECT_CONTEXT §13.3).
