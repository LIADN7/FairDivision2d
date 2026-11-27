# Help, Labels, Start and 50 % Tooltip Batch — Evidence

Captured 2026-11-27 on `feat/ux-help-tooltip-batch`, Unity 6000.2.10f1. Test mode only: Photon pool `uxtest_2.42`, Unity Services `development` / profile `uxtest`; no production connection. Context: [PROJECT_CONTEXT.md §17](../PROJECT_CONTEXT.md#17-current-status-and-handoff) "Help, labels, start and 50 % tooltip batch".

**Sources.** Editor images: Game view camera render (`capture game_view --source camera`, so the TEST MODE overlay badge is not in them) under a test-only Play Mode harness (empty scene, Photon `OfflineMode`, local `Config`; never committed). WebGL images: the Development build `Builds/uxtest_tooltip1`, served only by `tools/webgl_test/serve_test_build.py --mode online` and driven in a headless Chromium with real mouse events (viewport 964×569, so the canvas top is cut by a few pixels). Test identities `QA-Web` (Player 1) and `QA-Editor` (Player 2), run `20261127-151148-2bc67d`.

| File | What it shows |
|---|---|
| `01_help_page_960x600.jpg` | Help page opened from "?" (Editor, 960×600): rules text unchanged, the map legend copy (Hebrew levels, current heat colours) and the new neutral map illustration. |
| `02_help_page_1920x1080.jpg` | Same at 1920×1080. |
| `03_webgl_tooltip_hover.jpg` | WebGL practice, own cut phase: real mouse hover on the "i" icon next to "שרטטו קו על המפה" shows "למה כדאי לחלק 50/50?" and the approved body; the start note no longer contains the 50 % clause. |
| `04_opponent_mode_back_label.jpg` | Editor, Info round, opponent map shown: the toggle reads "חזרה לערכים שלי" in the existing deep-red mode; the cut plate, its icon and the Cut button are hidden. (Own view: "הצג את ערכי השחקן השני".) |
| `05_practice_starts_on_step0.jpg` | Practice right after load: no Help page; tutorial Step 0 is shown and the cut plate (with its icon) is hidden while the step locks the game. |
| `06_webgl_round1_start_no_help.jpg` | WebGL, real round 1 (Normal) right after the practice → round transition: game screen directly, cut plate with the info icon. |
| `07_webgl_round1_regions.jpg` | Round 1 after both cuts, Player 1: cards 19.43 / 25.30 / 25.61 / 29.66 % = the values recomputed from the Editor client's cells; no info icon in the choose phase. |
| `08_webgl_round1_result.jpg` | Round 1 result, Player 1: blue (chosen) + yellow (remainder) = 55.3 %; Player 2 (Editor) received red + green = 54.1 %. |
