# Participant Text Inventory and Microcopy Proposals (Game-feel Stage 3)

> Status 2026-11-12, branch `feat/game-feel-visual-polish`. Inventory of the participant-facing texts in the lobby, gameplay and waiting states, tutorial, Help, result presentation and `EndGameFlow`, with a research classification. **No participant-facing string was changed in Stage 3 or Stage 4** (exact list of changed strings: *none*). Stage 3 changed presentation only (grouping, hierarchy, typography). Every wording idea below is a **proposal that needs research-owner approval**.
>
> How the list was produced: every `m_text` / `m_Text` in `MainScene`, `CutSceneAITutorial`, `CutScene`, `EndGameFlow` and `Assets/Objects/SessionDialogs.prefab`, plus every Hebrew string literal in `Assets/Script` (178 entries; script `inv.py` in the session scratchpad). The same extraction after the batch gives the same 178 strings; the only diff is mechanical — the C# literal `"\nסבב "` in `EndGamePlayerDataManager` became `"\n" + "סבב …"`, and the displayed sentences are byte-identical.

## Classification key

| Class | Meaning | Rule in this batch |
|---|---|---|
| **A** | Research-critical instruction (50 % guarantee, valuation subjectivity, information/chat conditions, payment and finishing) | Unchanged, verbatim |
| **B** | Gameplay rule or required explanation (cut, 1-2-1 selection, allocation and result display) | Unchanged |
| **C** | Operational status / message (waiting, connecting, errors, dialogs) | Change only if clearly equivalent and shorter |
| **D** | Decorative or repeated UI text (headings, labels) | Change only if unambiguous |

## Inventory by screen

### Lobby (`MainScene`, `Launcher.cs`) — English, legacy `Text`

| Text | Source | Class |
|---|---|---|
| `Fair Division Game` | scene `LobbyTitle` | D |
| `1. Enter your name.` / `2. Choose a room.` / `3. You will play with the other player who chooses the same room.` | scene `LobbyInstructions` | C (pairing procedure) |
| `Enter Your Name...` | `NameField` placeholder | C |
| `Room N  (k/2 players)` | `Launcher.Start`, `OnRoomListUpdate` | C |
| `Connecting to the game server...` / `Loading the lobby...` / `Joining the room...` | `Launcher` | C |
| `This room is full or unavailable.` / `Please choose another room.` | `Launcher.OnJoinRoomFailed` | C (error) |
| `השחקן השני יצא מהמשחק.` / `לא ניתן להמשיך במשחק הנוכחי.` / `חזרה למסך הראשי` | `GameSessionDialogs` (lobby partner-exit notice, Batch 6) | C |

### Gameplay and waiting (`CutScene`; practice fork uses "המחשב" for "השחקן השני")

| Text | Source | Class |
|---|---|---|
| `אנא חלקו את המפה לשני חלקים (אם תחלקו לשני חלקים שווים, מובטח שתקבלו לפחות 50%)` + `אפשר לשרטט מחדש עד שתלחצו על אישור החלוקה.` | `Player` note | **A** (50 % guarantee) |
| `המפה מחולקת ל־50%` + `(אם תרצו, תוכלו לשנות את החלוקה או לבחור שלא לחלק באופן שווה)` | `Player` | **A** |
| `שרטטו קו על המפה` / `לחצו על אישור החלוקה` | `UpdatePhaseInstruction` | B |
| `בחרו חלק אחד` / `בחרו שני חלקים` / `בחרו חלק נוסף` | `UpdatePhaseInstruction` | B (1-2-1) |
| `שחקן 1 בחר חלק אחד.` `תורכם לבחור שני חלקים.` / `שחקן 2 בחר חלק אחד.` `המתינו לבחירת השחקן השני.` | `Player` | B |
| `חלק N - צבע`, `שווה לכם: `, `שווה לשחקן השני: `, `נבחר על ידכם`, `נבחר על ידי שחקן N`, `קיבלתם את החלק האחרון שנשאר`, `החלק שנשאר - שלכם` / `- לשחקן 1` | `Player` region labels | B |
| `הצד האדום` / `הצד הירוק`, `שרטטו קו כדי לראות את השווי` | `Player` readouts | B / C |
| `האחוזים מציינים את שווי השטח עבורכם - לא את גודלו.` `ייתכן שעבור השחקן השני הערך שונה.` | scene value caption | **A** (subjective valuations) |
| `שווי המשבצת עבורכם` / `שווי המשבצת עבור השחקן השני`, `נמוך` `בינוני` `גבוה` | `Manager` legend | B |
| `מוצגים הערכים שלכם` / `מוצגים הערכים של השחקן השני`, `הצגת מפת החום של השחקן השני` | `Manager`, scene | **A** (Info condition) |
| `הקו השחור: החיתוך שלכם` / `הקו הלבן: החיתוך של השחקן השני` | `Player` | B |
| `קיבלתם:` + breakdown, `You get total: x%` (English legacy) | `Player`, `Manager` | B |
| `צ'אט עם השחקן השני`, `אפשר לשוחח עם השחקן השני.`, `סמנו אם הגעתם להסכמה בצ'אט.` | scene `ChatLayer` | **A** (Chat condition, saved `isAgreed`) |
| `כתבו הודעה כאן...`, `שליחה`, `אני`, `השחקן השני` | scene, `Player`, `RtlChatInputWebGL` | C |
| `ממתינים לחיתוך של השחקן השני`, `ממתינים לבחירת השחקן השני`, `ממתינים לשחקן השני`, `עוברים לסבב הבא`, `עוברים לסיום המשחק` | `Player` waiting overlay | C |
| `ממתינים להתחברות השחקן השני!` | `Player` note (cut while alone in the room) | C |
| `השחקן השני ממשיך במשחק...` | `Player.GameOverClicked` note | C |
| `אישור החלוקה`, `המשך`, `יציאה` | buttons | C / D |
| `Player name and ID` / `This is round number N` (English legacy) | `Player` | D |
| Help page (`הוראות המשחק`, bulleted rules incl. 1-2-1 and the 50 % sentences, `חזרה למשחק`) | scene Help panel | **A** |
| `לצאת מהמשחק?`, `ההתקדמות בסבב הנוכחי לא תישמר.`, `ביטול`, `יציאה` | `SessionDialogs` exit dialog | C |
| `החיבור לשחקן השני נותק.`, `החיבור לשרת המשחק נותק.`, `לא ניתן להמשיך בסבב הנוכחי.`, `לא ניתן להמשיך במשחק.` | `SessionDialogs` connection notice | C |

### Tutorial (`CutSceneAITutorial`)

| Text | Source | Class |
|---|---|---|
| Step 0: `גררו קו ישר אחד שמחלק את המפה לאדום וירוק.` / `חלוקה 50/50 לפי הערכים שלכם מבטיחה לכם לפחות 50%.` / `המטרה: לקבל חלקים ששווים לכם כמה שיותר.` / `בסיום לחצו על אישור החלוקה.` (as displayed; the scene stores the numbers digit-reversed, `05/05` and `%05`, for TMP's right-to-left mode) | `Step0_GameIntro` | **A** |
| Step 1 P1 / P2: `המפה מחולקת כעת ל-4 חלקים.` + 1-2-1 sentence with "המחשב" | `Step1_CutIntroP1` / `P2` | B |
| `בסיום הבחירה יוצג סך החלק שלכם באחוזים, לפי הערכים שלכם.` | `Step2_EndGame` (exists, **not wired**) | B |
| `סבב תרגול` | round label, card heading | D |
| Practice Help page (`* בסבב התרגול אתם משחקים מול המחשב. הסבב אינו נכלל בחישוב התשלום.` …) | scene | **A** |

### EndGameFlow

| Text | Source | Class |
|---|---|---|
| `המשימה הושלמה!` / `תודה על השתתפותכם!` / `לחצו על "שלח" כדי לסיים את המשחק` / `ולקבל את התגמול שלכם ב-Panel4All` (stored as `…ב-llA4lenaP` so TMP's right-to-left mode shows `Panel4All`) | scene `Explation` | **A** (finish and payment) |
| `התגמול שלכם` | scene | D |
| `X.XX ₪` (display rounding, unrounded value stored) | `EndGamePlayerDataManager.SetWinnings` | B (payment display) |
| `התגמול חושב לפי סבב אחד שנבחר באקראי.` | `SetWinnings` | **A** |
| `סבב N נבחר לחישוב התגמול.` | `SetWinnings` | **A** |
| `שלח` | Finish button (named in the instruction above) | **A**-adjacent |
| `יציאה` | Home button | C |
| `Enter Your Name...` | `NameField` placeholder | C |

## What Stage 3 changed (presentation only, no string change)

| Screen | Change |
|---|---|
| Lobby | Same English strings, regrouped: title and accent above one card; steps on the left, name field and rooms on the right; the room-full message gets a coral plate that appears only while Launcher has written it. |
| EndGameFlow | The instruction keeps its four lines verbatim and gains a check badge. The amount sits in a teal hero card. **The round sentence (`סבב N נבחר לחישוב התגמול.`) moves out of the explanation block into its own highlighted chip directly under the amount; the explanation (`התגמול חושב לפי סבב אחד שנבחר באקראי.`) follows below it.** The two sentences are unchanged, but they now display in the opposite order (round first). The chip is shown under the same condition as before (`RandArrays` holds the drawn index). |
| Dialogs | Same strings; Rubik typography, the safe choice ("ביטול") is the teal primary button, "יציאה" is secondary. |

## Deferred wording proposals (owner approval needed — not implemented)

| # | Before | Proposed | Reason |
|---|---|---|---|
| P1 | Lobby in English: `Fair Division Game`, the three steps, `Enter Your Name...`, `Connecting to the game server...`, `Loading the lobby...`, `Joining the room...`, `This room is full or unavailable. Please choose another room.`, `Room N  (k/2 players)` | `משחק חלוקה הוגנת`; `1. הקלידו את שמכם.` `2. בחרו חדר.` `3. תשחקו עם השחקן השני שבוחר באותו חדר.`; `הקלידו את שמכם...`; `מתחברים לשרת המשחק...`; `טוענים את הלובי...`; `מצטרפים לחדר...`; `החדר מלא או לא זמין.` `בחרו חדר אחר.`; `חדר N  (k/2 שחקנים)` | Every other screen is Hebrew. Technical note: the lobby texts are legacy `Text` (no right-to-left); Hebrew needs TMP/RTLTMPro, and typing a Hebrew name has the same limits as the chat field (PROJECT_CONTEXT §17, Batch 5). |
| P2 | Finish button `שלח` and the instruction `לחצו על "שלח" כדי לסיים את המשחק` | `סיום` and `לחצו על "סיום" כדי לסיים את המשחק` (always together) | The button finishes the session; "send" is ambiguous. This is a class A text, so both must change together or not at all. |
| P3 | EndGameFlow name field: unlabeled, prefilled, editable, never read by Finish | Either hide it, or add the label `השם שלכם` and make it read-only | It looks like a required input but has no effect. |
| P4 | `ממתינים להתחברות השחקן השני!` | `ממתינים להתחברות השחקן השני` | Matches the other waiting messages (no exclamation mark). |
| P5 | `השחקן השני ממשיך במשחק...` (replaces the result breakdown on the note) | Keep the breakdown and show `השחקן השני לחץ על המשך` in the waiting overlay instead | The note currently hides the player's own result (pre-existing; behaviour change). |
| P6 | English legacy `You get total: x%`, `This is round number N`, `Player name and ID`, cut readouts | Hebrew via the existing TextRTL pattern, e.g. `סך הכול קיבלתם: x%`, `סבב N` | Known issue (PROJECT_CONTEXT §17). |
| P7 | Tutorial proposals from the parity pass (card headings, Step 0 last sentence, `Step2_EndGame`, practice Help "שחקן 1 / שחקן 2", repeated 50 % sentence) | See PROJECT_CONTEXT §17 "Deferred text proposals" | Still not approved; nothing implemented. |
