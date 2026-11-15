# WebGL Development Build Safety — Evidence

Captured 2026-11-16 on `fix/webgl-development-safety` (runtime tree = `dev` `dc28f92`), Unity 6000.2.10f1. Test mode only: Photon pool `uxtest_2.42`, Unity Services `development` / profile `uxtest`. No production connection, no deployment. Procedure and context: [PROJECT_CONTEXT.md §13.2](../PROJECT_CONTEXT.md#132-test-mode-isolation-from-participants) and §17 "WebGL Development build safety".

**Clients.** WebGL Development build `Builds/uxtest_safety1` served only by `tools/webgl_test/serve_test_build.py` on `127.0.0.1:8765` — offline mode in the in-app browser (image 01), online mode in a headless Chromium (images 02–13, canvas 960×600, software WebGL, real mouse events). Editor Play Mode as the second player (Game view captures at 1280×720). Test identities `QA-Web` (Player 1) and `QA-Editor` (Player 2), room `Room_3`, run `20261115-222849-c8d590`.

| File | What it shows |
|---|---|
| `01_offline_gate_all_requests_refused.jpg` | Pre-connection gate, offline mode: TEST MODE badge, engine "Development Build" watermark; the Development Console shows the Photon name-server socket and Unity Authentication refused by the harness ("uxtest harness: offline mode"). |
| `02_webgl_practice_hover_smooth_contour.png` | WebGL practice, choose phase: hovering the green card outlines its region with smooth contours and dims the others. |
| `03_editor_p2_no_early_reveal_round1.png` | Round 1 (Normal), Editor = Player 2 right after Player 1 confirmed its cut: no Player 1 line or colouring is visible. |
| `04_webgl_p1_round1_regions.png` | Round 1 after both cuts, Player 1: cards 38.66 / 38.56 / 14.88 / 7.89 % = the values recomputed from the Editor's state. |
| `05_webgl_p1_round1_result.png` | Round 1 result, Player 1: green + blue = 46.5 %; own regions with smooth amber contours, Player 2's regions veiled and locked. |
| `06_webgl_infoachat_own_view.png` | InfoAChat round, Player 1, own view with a drawn line (toggle, value plate, chat). |
| `07_webgl_infoachat_opponent_mode.png` | Same moment in the opponent-map mode: deep-red frame and toggle ring, deep-red plate, rose titles, dark-red card behind the readouts (62.45353 / 37.54647 % = Player 2's values for this line); Cut button hidden. |
| `08_webgl_infoachat_own_view_after_return.png` | Back to the own view: pixel-identical to image 06 over the whole canvas (0 pixels differ by more than 24/255). |
| `09_editor_p2_infoachat_opponent_mode.png` | Player 2 (Editor) in the opponent-map mode before its own cut. |
| `10_webgl_infoachat_toggle_gone_after_own_cut.png` | Player 1 after its own cut: toggle and value plate gone, waiting for Player 2's cut; chat message from Player 2 received. |
| `11_webgl_infoachat_hover_contour.png` | InfoAChat choose phase, hovering the yellow card: both separate yellow pieces outlined. |
| `12_webgl_p1_infoachat_result.png` / `13_editor_p2_infoachat_result.png` | InfoAChat results on both clients: P1 red + blue = 46.6 %, P2 yellow + green = 67.4 %; allocations mirror each other. |

**Harness summaries (sanitized; raw logs not committed because they contain project and test-player IDs).**

Offline mode (network-restricted):

| Attempted destination | Via | `UnityEnvironment` header | Decision |
|---|---|---|---|
| `wss://ns.photonengine.io:19093/` | WebSocket | — | refused (offline) |
| `https://player-auth.services.api.unity.com/v1/authentication/session-token` (×3, SDK retries) | fetch | `development` | refused (offline) |
| `https://uxtest-csp-probe.invalid/stream` (deliberate probe, bypassing the guard) | EventSource | — | blocked by the browser CSP (`connect-src`), reported to the local server |

Console before any connection: `TEST MODE: Photon AppVersion = uxtest_2.42 (lobby load, connected=False)`, `TEST MODE: Photon AppVersion = uxtest_2.42`, `TEST MODE: Unity Services environment = development, profile = uxtest`; Unity Services then failed with a network error and nothing retried without the environment header.

Online mode (both page loads of the session):

| Destination | Via | `UnityEnvironment` | Decision | Count |
|---|---|---|---|---|
| `wss://ns.photonengine.io:19093/` | WebSocket | — | allowed after `uxtest_2.42` was logged; outgoing frame carried `uxtest_2.42` | 3 |
| `wss://gcams1128.exitgames.com/Master/` | WebSocket | — | allowed | 3 |
| `wss://gcams2015.exitgames.com/game/`, `wss://gcams2020.exitgames.com/game/` | WebSocket | — | allowed | 1 each |
| `https://player-auth.services.api.unity.com/v1/authentication/anonymous` and `/session-token` | fetch | `development` | allowed | 1 + 2 |
| `https://cloud-save.services.api.unity.com/v1/data/projects/<project>/players/<player>/item-batch` | fetch | — | refused (Cloud Save not enabled for this test) | 3 |

STOP events: none. No other host was contacted.

**Build guard (Editor Console).**

```
[DevelopmentBuildGuard] REFUSED: this build is labelled as a test build but its BuildOptions do not contain Development, …
target=WebGL, options=ForceOptimizeScriptCompilation, Il2CPP, CompressTextures, StripDebugSymbols, ShaderLivelinkSupport, DetailedBuildReport,
active profile='Web - Desktop - Development', Development Build setting=True, output=…/Builds/guardtest_incident

[DevelopmentBuildGuard] Development (TEST MODE) build accepted: target=WebGL, options=ForceOptimizeScriptCompilation, Il2CPP, CompressTextures,
StripDebugSymbols, ShaderLivelinkSupport, Development, DetailedBuildReport, active profile='Web - Desktop - Development', Development Build setting=True,
output=…/Builds/uxtest_safety1
```

The first line is the exact incident command (`build run --profileName "Web - Desktop - Development"` without `--options`), refused before compiling; the second is the approved command.
