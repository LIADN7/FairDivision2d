# Visual Polish — Review Package

Captured 2026-11-15 on `feat/ux-clarity-batch` (local commit, not pushed), Unity 6000.2.10f1 Editor, Game view 1920×1080, camera render (`capture game_view --source camera`, so Screen Space - Overlay UI such as the TEST MODE badge is not in the images).

**Runtime harness (test only, never committed):** Play Mode from an empty scene, Photon **`OfflineMode`** (no Photon server connection at all), a local `Config` (practice; CutScene `createConfig(2, 2)` = Info, round index 2). Lines were finished through each line manager's own `FinishDrawing`, buttons pressed with their real `onClick`, hovers sent as real EventSystem pointer events. The practice computer's random cut was seeded (`Random.InitState(3)` → its cut #0) so the before and after runs share the same partition. Before images are from the unmodified branch head (`0e3aac2`), after images from this change. Unity Services still initialise in the `development` environment (test mode) when a game scene starts; nothing was saved.

| File | What it shows |
|---|---|
| `01_opponent_mode_before_after.jpg` | CutScene Info round, same drawn line. Before / after × own view / other player's values. |
| `02_opponent_mode_detail.jpg` | Close-up of the mode: deep-red frame with outer glow, dimmed board behind the cells, rose map title, toggle with deep-red fill and light ring, bordered plate. |
| `03_opponent_mode_transition.jpg` | The 0.17 s transition held at 35 % and 65 % (via the same `Apply(m)` the tween drives), the full mode, and the return to the own view. |
| `04_region_hover_before_after.jpg` | Practice choose phase, blue card hovered: per-cell stair-step outline (before) vs smooth contour with soft halo and lighter dimming (after). |
| `05_outline_detail_zoom.jpg` | 2× crop of the same hover (Donegal, Tyrone, Fermanagh). |
| `06_region_owned_before_after.jpg` | After the choices (green chosen, yellow remainder; computer red + blue): amber per-cell edges vs smooth amber contours; the computer's regions keep the same veil. |
| `07_practice_states.jpg` | Practice after the change: no hover, hover green, hover fade held at 40 %, hover after the choices. |

**Same values before and after (display-only check):** practice cards 26.82 / 31.68 / 17.31 / 24.19 %, received green 31.7 % + yellow 17.3 % = 49.0 %; CutScene own readouts 44.12955 / 55.87045 %, other player's 31.97026 / 68.02974 %, 187 / 287 cells per side; `CountPlayerSeeOther` 0 → 1 → 2 per toggle as before; returning to the own view restores every colour exactly; the Help page hides the frame, glow and toggle with the map.
