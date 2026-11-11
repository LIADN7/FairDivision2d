# Game-feel batch — visual evidence (Stages 1–2, CutScene)

Left = before (`dev` `ce2350d`), right = after (`feat/game-feel-visual-polish`).

| File | What it shows | Source |
|---|---|---|
| `01_region_selection_your_turn_before_after.jpg` | Choose phase, Player 1's turn | Edit-mode staged state* |
| `02_taken_states_player2_before_after.jpg` | Player 2: region taken by Player 1, region taken by you, second pick | Edit-mode staged state* |
| `03_waiting_state_before_after.jpg` | Player 1 waiting for Player 2's choice | Edit-mode staged state* |
| `04_result_before_after.jpg` | End of round: ownership badges, note, result, Next | Edit-mode staged state* |
| `05_cut_phase_960x600_before_after.jpg` | Cut phase at the WebGL test size | Edit-mode staged state* |
| `06_help_page_before_after.jpg` | Help page | Edit-mode capture (layers as at runtime) |
| `07_runtime_editor_player1_cut_turn_result.jpg` | Player 1 (Editor, Play Mode): cut, your turn, waiting, result | Runtime, two-client test mode |
| `08_runtime_webgl_player2_960x600.jpg` | Player 2 (WebGL Development, real mouse): line, choose, after first pick, result, waiting for Next, exit dialog | Runtime, two-client test mode |
| `09_runtime_webgl_selection_motion_frames.jpg` | Frames around a real click: hover outline → flash and outline grow → amber outline → check badge | Runtime WebGL |
| `10_runtime_webgl_waiting_entrance_frames.jpg` | Waiting plate settling in and the cards' "values ready" flash after both cuts | Runtime WebGL |
| `11_runtime_chat_help_transition.jpg` | Chat round (Editor + WebGL message), Help on WebGL, round transition | Runtime |

Tutorial (practice, `CutSceneAITutorial`) — added in the tutorial pass:

| File | What it shows | Source |
|---|---|---|
| `12_tutorial_step0_explanation_before_after.jpg` | Step 0 explanation (cut instructions, map spotlight) | Edit-mode staged state* |
| `13_tutorial_step1_player1_before_after.jpg` | Step 1, Player 1 wording (region spotlight) | Edit-mode staged state* |
| `14_tutorial_step1_player2_before_after.jpg` | Step 1, Player 2 wording | Edit-mode staged state* |
| `15_tutorial_gameplay_cut_960x600_before_after.jpg` | Practice cut phase at the WebGL test size | Edit-mode staged state* |
| `16_tutorial_region_selection_before_after.jpg` | Practice choose phase, your turn | Edit-mode staged state* |
| `17_tutorial_waiting_before_after.jpg` | Waiting for the computer's choice | Edit-mode staged state* |
| `18_tutorial_result_before_after.jpg` | Practice result / completion state | Edit-mode staged state* |
| `19_tutorial_runtime_editor_walkthrough.jpg` | Editor Player 1 run: Help auto-open, Step 0, Step 1, your turn, after the pick, result | Runtime (test mode) |
| `21_tutorial_runtime_webgl_player2.jpg` | WebGL Player 2 (real mouse): Step 0, Step 1 (P2 wording), choices, result | Runtime (WebGL Development) |
| `22_tutorial_to_round1_transition.jpg` | Both players finish practice (Editor P1 result, WebGL P2 waiting) → first real round (WebGL after both cuts, WebGL and Editor results) | Runtime (two clients) |

The tutorial step entrance (0.22–0.30 s) finishes before an Editor capture returns, so no still shows it; it uses the same `UiAppear` component whose frames are shown for CutScene in sheets 09–10.

\* Staged: CutScene opened in the Editor and put into the state with exactly the texts and interactable values that `Player` / `Manager` write (never saved). Map cells show their edit-time colour there (no runtime shading); the runtime sheets show real shading and lines. Screen-space-overlay dialogs (exit / connection notice) do not appear in camera captures; sheet 08 shows the exit dialog from WebGL.

To see the motion live: run a test-mode two-client round (`docs/PROJECT_CONTEXT.md` §13.2) or Play Mode in the Editor. F8 toggles the UI effects in the Editor and in Development builds.
