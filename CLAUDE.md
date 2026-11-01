# CLAUDE.md

Unity 6 (6000.2.10f1) + Photon PUN 2 research game for two-player fair division (Symmetric Cut and Choose).

## Read first

- **[docs/PROJECT_CONTEXT.md §17 — Current Status and Handoff](docs/PROJECT_CONTEXT.md#17-current-status-and-handoff)** — current branch, finished UX batches, known issues, next batch.
- **[docs/PROJECT_CONTEXT.md](docs/PROJECT_CONTEXT.md)** — project overview, experiment conditions and schedule, data schema, environment, Unity Editor access, **test mode** (§13.2) and **release checklist** (§13.3). Read it before planning any change.
- **[docs/CUT_AND_CHOOSE_REFERENCE.md](docs/CUT_AND_CHOOSE_REFERENCE.md)** — algorithm implementation, state machines, synchronization, dependency map, high-risk coupling points and research invariants. Read it before touching gameplay, UI flow, networking or data code.

## Development priorities

- **Primary objective: improve the player's UX/UI** — clarity, instructions, feedback, readability — with minimal risk to gameplay and research results.
- **Preserve research behavior and results.** Never change selection rules, valuations, experimental conditions, synchronization, data logging or reward calculations without explicit research-owner approval.
- **Prefer minimal, isolated changes** to existing scenes and scripts. Avoid general refactoring or architecture redesign; refactor only when strictly required by an approved feature or bug fix, and say so in the plan.
- **Every implementation includes regression verification** appropriate to its risk (e.g. both clients show the same regions, the 1-2-1 order, identical percentages and saved values before/after), and the plan states how it will be checked.
- **`main` stays protected:** work on a feature branch; changes reach `main` only through a reviewed PR.

## Working rules

- **Preserve research logic.** Do not change anything listed in *Research invariants* (CUT_AND_CHOOSE_REFERENCE §14) — rules, valuations, round schedule, information/chat conditions, payment, data schema, participant-facing texts — without explicit approval from the research owners, even during UX work. If code and the stated rules disagree, report the discrepancy and ask; do not silently resolve it.
- **Inspect real assets before editing.** Behavior is split between C# and serialized scenes/prefabs (button wiring, cell valuations, inspector values). Check the relevant `.unity` / `.prefab` data, not only scripts. Never edit `.meta` files or GUIDs by hand.
- **Mirror shared logic.** The practice scene uses forked copies of the core classes (`Assets/Script/AITutorial/*`); rule changes usually need to be applied to both.
- **Ask before significant architecture changes** (refactors across classes, new packages, changes to `Packages/` or `ProjectSettings/`, networking or persistence changes).
- **Git:** never push or merge directly into `main`, and never alter `main` or `dev` without explicit instruction. Do not assume a branch workflow — check the current branch and ask when unsure. Preserve uncommitted work.
- **Secrets:** never print, copy or commit credentials, API keys, Photon App IDs or Cloud service keys (some already exist in the repo and are treated as exposed — see PROJECT_CONTEXT §11).
- Do not start Play Mode, builds or external services (Photon, Unity Cloud, survey redirect) unless asked. When runtime testing is approved, test only in **test mode** (Editor or Development build: separate Photon pool, `development` Unity Services, TEST MODE badge) and never against production; do not change the isolation code without approval.
- **Release safety:** participant builds use the Release profile with Development Build OFF and no TEST MODE badge (PROJECT_CONTEXT §13.3). Never publish a Development/test build to participants.
- **Agents may commit and push feature branches; agents never merge into `main`.**
