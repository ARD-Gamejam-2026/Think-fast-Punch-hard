# Quiz UI — Design

Date: 2026-08-22
Status: Approved design, pending implementation plan

## Goal

A reusable quiz screen for "Think fast, Punch hard": a question at the top
(text plus an optional image), four A/B/C/D answer buttons below, and a
per-question countdown timer. The player answers by clicking/tapping a
button. Other game systems (scoring, level flow) hook in via an event; they
are out of scope for this piece of work.

## Requirements

- Question = text + optional image. When a question has no sprite, the image
  panel is hidden and the layout collapses around it.
- Exactly four answer choices, labeled A–D.
- Input: mouse/touch click on the answer buttons (uGUI `Button`). Keyboard /
  gamepad support may come later via the Input System; nothing in this design
  blocks it.
- Per-question countdown timer, visible as a shrinking bar. Reaching zero
  ends the question.
- **Three distinct end states** — these are separate results, never merged:
  - **Correct** — player clicked the right answer. Correct button turns green.
  - **Wrong** — player clicked a wrong answer. Clicked button turns red, the
    correct button is highlighted (gold) so the player learns the answer.
  - **TimedOut** — timer reached zero with no click. No button is red; the
    correct button is highlighted (gold) and the timer bar shows empty/alarm
    color.
- After the question resolves, further clicks and timer ticks are ignored.
- Feedback stays on screen for a short configurable delay, then a
  `QuestionAnswered(QuizResult)` event fires for future scoring/sequencing.

## Tech choices

- **uGUI (Canvas + TextMeshPro + Button)**, not UI Toolkit — better WebGL
  track record and faster jam iteration.
- **Prefab built by a one-time editor script** (`Tools > Quiz > Create Quiz
  Panel`) rather than hand-authored scene YAML or runtime-generated UI. After
  generation it is a normal prefab, freely restylable in the editor; all
  component references are wired by the builder.
- **Questions as ScriptableObject assets** — fast content authoring during
  the jam, supports `Sprite` references natively.

## Components

All gameplay scripts in `Assets/Scripts/Quiz/`, compiled into
`Assembly-CSharp` (no asmdef, matching project convention). Editor script in
`Assets/Scripts/Quiz/Editor/`.

### `QuizResult` (enum)

`Correct`, `Wrong`, `TimedOut`.

### `QuizQuestion` (ScriptableObject)

- `string questionText`
- `Sprite image` (optional, may be null)
- `string[] answers` (length 4)
- `int correctIndex` (0–3)
- `float timeLimitSeconds`

Created via `CreateAssetMenu`; question content assets live under
`Assets/Quiz/Questions/`.

### `QuizSession` (plain C# class — the testable core)

No UnityEngine scene/component dependencies. Holds the state of one question
in progress.

- Constructed with: correct index, time limit.
- `SelectAnswer(int index)` — resolves the session to `Correct` or `Wrong`.
- `Tick(float deltaTime)` — counts down; at zero resolves to `TimedOut`.
- `RemainingTime` / `NormalizedTimeRemaining` — for the timer bar.
- `IsResolved` / `Result` — exactly-once resolution; `SelectAnswer` and
  `Tick` are no-ops after resolution.

### `QuizView` (MonoBehaviour on the prefab)

Pure presentation; no game rules.

- Serialized references: question label (TMP), image panel + `Image`,
  timer fill `Image`, four `AnswerButton`s.
- `ShowQuestion(QuizQuestion)` — sets texts, shows/hides image panel,
  resets all button visuals.
- `SetTimerFill(float normalized)`.
- `ShowResult(QuizResult, int selectedIndex, int correctIndex)` — applies
  the three feedback treatments described in Requirements.

### `AnswerButton` (MonoBehaviour, one per choice)

- Serialized references: `Button`, letter label (A–D), answer label,
  background `Image`.
- Visual states: `Normal`, `Correct` (green), `Wrong` (red),
  `Highlighted` (gold — used for revealing the answer on Wrong/TimedOut).
- Raises a click callback with its index; interactability toggled by the view.

### `QuizController` (MonoBehaviour glue)

- Serialized: `QuizView`, feedback delay seconds, optional starting
  `QuizQuestion` for play-mode testing.
- `ShowQuestion(QuizQuestion)` — creates a `QuizSession`, drives the view.
- `Update()` — forwards `Time.deltaTime` to `QuizSession.Tick`, updates the
  timer bar.
- On resolution: calls `QuizView.ShowResult`, waits the feedback delay,
  then raises `event Action<QuizResult> QuestionAnswered`.

### `QuizPanelBuilder` (editor script)

Menu item `Tools > Quiz > Create Quiz Panel`. Programmatically builds the
canvas hierarchy (question panel, image slot, timer bar, four answer
buttons), wires every serialized reference, and saves
`Assets/Quiz/QuizPanel.prefab`. Idempotent enough to re-run (overwrites the
prefab). One-time generator — after that, styling changes happen in the
editor, not in this script.

## Data flow

Click / timer tick → `QuizController` → `QuizSession` (rules) →
`QuizController` reads resolution → `QuizView`/`AnswerButton` (feedback) →
delay → `QuestionAnswered(QuizResult)` event.

## Error handling

- `QuizQuestion` `OnValidate` clamps `correctIndex` to 0–3 and warns when
  answers are missing/empty or the time limit is non-positive.
- `QuizController.ShowQuestion(null)` logs an error and does nothing.
- Post-resolution input/ticks are ignored by `QuizSession` (tested).

## Testing

EditMode tests (`Assets/Tests/EditMode/QuizSessionTests.cs`) for
`QuizSession`:

- correct pick resolves `Correct`
- wrong pick resolves `Wrong`
- ticking past the limit resolves `TimedOut`
- answering just before zero still resolves `Correct`/`Wrong`
- inputs and ticks after resolution change nothing (result fires once)
- `NormalizedTimeRemaining` goes 1 → 0

Run with the batch-mode command from CLAUDE.md, plus a headless compile
check. Visual verification: `SampleScene` gets a `QuizPanel` instance and a
sample question asset so pressing Play shows a working question immediately.

## Out of scope (v1)

Scoring, question sequencing/database, win/fail meta flow, keyboard/gamepad
input, sound, animations beyond simple color feedback.
