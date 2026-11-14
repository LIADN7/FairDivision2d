# UX Clarity Batch — Evidence

Captured 2026-11-14 on `feat/ux-clarity-batch`, **test mode only** (Photon `uxtest_2.42`, Unity Services `development` / profile `uxtest`, TEST MODE badge; no production connection, no survey redirect). Editor captures render the game camera (`capture game_view --source camera`), so the Screen Space - Overlay TEST MODE badge is not in them; WebGL panels are screenshots of the local Development build `Builds/uxtest_ux1` in the in-app browser pane (canvas scaled to the pane, real mouse).

| File | Panels (left → right, top → bottom) |
|---|---|
| `01_heatmap_before_after.jpg` | Practice cut phase, Player 1, same live cells (1920×1200 render): **before** = the old inner-square formula re-applied by a display-only harness, with the old legend PNGs; **after** = this branch (`HeatShading`, regenerated legend). |
| `02_cut_line_short_drag_before_after.jpg` | Row 1 (Editor practice, the same 0.34-unit drag, identical readouts 44.02634 / 55.97166 %): **before** = extension hidden by pausing the line manager's sync for one frame; **after**. Row 2 (WebGL, real mouse): practice ~30 px drag, round 1 ~30 px drag — both shown edge to edge. |
| `03_region_hover_spotlight.jpg` | Hover spotlight: Editor practice (green card), WebGL round 1 (green card, real hover; values equal the Editor's independent recompute with Player 1's valuation), WebGL practice (yellow card). |
| `04_region_ownership_marks.jpg` | After the choices: Editor practice as Player 1 (red + yellow remainder outlined amber; the computer's regions veiled), Editor practice as Player 2; WebGL round 1 as Player 1, Editor round 1 as Player 2 (same allocation seen from both sides). |
| `05_info_mode_own_other_return.jpg` | Row 1 (WebGL round 3 `Info`, real clicks): own view → other player's view (frame, white toggle label, plate) → back to own view. Row 2: Editor (Player 2) other view in the same round; WebGL round 5 `InfoAChat` other view with the chat panel; WebGL round 3 after both cuts (toggle and frame gone, hover on red). |
| `06_viewport_sizes_1280_1600_1920.jpg` | Editor practice choose phase with the green card hovered at 1280×720, 1600×900 and 1920×1080. |
| `07_two_client_test_mode_lobby_and_end.jpg` | WebGL lobby with the TEST MODE badge and "Development Build"; WebGL end screen of the six-round run (46.31 ₪, round 1 drawn). |
