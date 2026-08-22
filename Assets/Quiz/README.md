# Quiz System

A question with four A–D answers, a countdown timer, and three possible
outcomes: **Correct** (right answer clicked, turns green), **Wrong** (wrong
answer clicked, turns red and the right one is revealed in gold), and
**TimedOut** (timer ran out, right answer revealed in gold, nothing red).

The code lives in `Assets/Scripts/Quiz/`:

| Piece | Role |
|---|---|
| `QuizQuestion` | ScriptableObject holding one question's content |
| `QuizSession` | Rules of one question in progress (plain C#, unit-tested) |
| `QuizView` / `AnswerButton` | Presentation on the `QuizPanel` prefab |
| `QuizController` | Glue: runs a question, raises `QuestionAnswered` |
| `QuizFlow` | Endless driver: rolls among authored/math/Wikipedia questions by weight |
| `MathQuestionGenerator` | Builds the random math questions (unit-tested) |
| `SequenceQuestionGenerator` | Builds number and shape sequence-completion questions (unit-tested) |
| `WikipediaQuestionSource` | Prefetches live Wikipedia photo questions for one topic list (places, animals, ...) |
| `WikipediaTopicList` | Curated Wikipedia titles + display names for one topic (`Landmarks.asset`, `Animals.asset`) |
| `Editor/QuizPanelBuilder` | One-time generators under **Tools > Quiz** |

## Adding a new question

1. In the Project window: **right-click > Create > Quiz > Question**
   (put it in `Assets/Quiz/Questions/`).
2. Fill in the inspector:
   - **Question Text** — shown at the top.
   - **Image** — optional sprite, shown between question and timer. The
     image area stays reserved either way, so the panel height never jumps
     between image and text-only questions.
   - **Answers** — exactly 4 entries, shown as A–D in order.
   - **Correct Index** — 0 = A, 1 = B, 2 = C, 3 = D.
   - **Time Limit Seconds** — countdown length; running out counts as
     TimedOut.

Empty answers log a warning in the console so they're hard to ship by
accident.

## Showing questions at runtime

The quickest way: drop the `QuizPanel` prefab into a scene and assign your
question to the controller's **Starting Question** field — it plays a
single question on scene start.

For a sequence, use the built-in `QuizFlow` component (this is how
`SampleScene` is set up): add it next to the `QuizController`, assign the
controller and a list of questions, and it cycles through them endlessly —
after the last question it wraps back to the first. Leave the controller's
**Starting Question** empty when a flow is driving it, or the first
question shows twice.

`QuizFlow` mixes in generated and prefetched question types alongside the
authored list using relative weights: **Authored Weight**, **Math
Weight**, and a **Wikipedia Sources** list where each entry pairs a
`WikipediaQuestionSource` with its own weight (defaults: authored 1, math
1, no Wikipedia sources). Each round rolls proportionally among whichever
types are currently available — authored questions are available whenever
the list is non-empty, math is always available when its weight is
non-zero, and a Wikipedia source only counts as available once it has a
prefetched question ready (its background fetch queue takes a moment to
fill, and it stays empty while offline). Add as many Wikipedia sources as
you like — the sample scene wires two, places (weight 2) and animals
(weight 2). With an empty question list and math enabled, every round is
math; if the only available weight momentarily has none ready (e.g.
Wikipedia-only while the queues refill), `QuizFlow` retries shortly
instead of stalling.

### Sequence questions

`SequenceQuestionGenerator` builds "what comes next" puzzles, split between
two families. **Number sequences** reuse the plain text question/answer
presentation and cover three patterns kept easy enough to solve under the
timer: arithmetic (constant step), geometric (x2/x3), and Fibonacci with
classic small starts (1,1 / 1,2 / 2,3 / 2,4 / 3,5). **Shape sequences**
render the prompt and all four answers as procedurally-drawn images via
`ShapeRenderer`, across four transforms: rotation (triangle only — square
and star read as rotationally symmetric), count (an increasing number of
shapes), shape-type cycle, and color/fill cycle.

The shape prompt lays its terms out in rows (at most four per row, so a
four-term sequence stays on one line and a six-term one wraps to 3x2) with
the "?" placeholder centered on its own row below; count sequences add a gap
between cells so the groups stay countable.

Both families mix into the same `QuizFlow` bucket, tuned by three inspector
fields: **Sequence Weight** (relative roll weight, default 1, alongside
authored/math), **Sequence Time Limit Seconds** (default 8), and
**Sequence Shape Share** (fraction of sequence rounds that are shapes
rather than numbers, default 0.5).

For custom behavior (scoring, lives, a win screen), write your own driver
against the same event:

```csharp
using ThinkFast.Quiz;

public class ScoredQuizFlow : MonoBehaviour
{
    [SerializeField] private QuizController quiz;
    [SerializeField] private QuizQuestion[] questions;

    private int current;
    private int score;

    private void OnEnable() => quiz.QuestionAnswered += OnAnswered;
    private void OnDisable() => quiz.QuestionAnswered -= OnAnswered;

    private void Start() => quiz.ShowQuestion(questions[current]);

    private void OnAnswered(QuizResult result, float speed)
    {
        if (result == QuizResult.Correct)
            score++;

        current++;
        if (current < questions.Length)
            quiz.ShowQuestion(questions[current]);
        // else: show your win/score screen here.
    }
}
```

## Reacting to results

`QuizController` raises two plain C# multicast events, each exactly once
per question, and both carry the result plus the answer speed (the timer's
normalized remaining time at resolution: **1 = answered instantly, 0 =
timed out**):

- **`QuestionResolved(QuizResult, float)`** — fires the *instant* the
  player clicks or the timer runs out, before any feedback delay. Hook
  instant gameplay rewards here (Flow meter, action points, damage
  windows) so they land the moment the player earns them.
- **`QuestionAnswered(QuizResult, float)`** — fires after the feedback
  colors have been on screen for the controller's **Feedback Delay
  Seconds** (default 0.5 s). `QuizFlow` uses this one to advance, so the
  result stays visible between questions.

The timer bar doubles as a speed indicator: it is green while a solve
still counts as fast (top 40 % of the time limit — the Flow-building
zone), yellow after that, and red in the last second. Zone colors and
thresholds are inspector fields on `QuizView`.

## Feeding the fighter

The fighter half is wired to these events by one component,
`QuizRewardBridge` (in `Assets/Scripts/Economy/`, on the generated quiz
root in the fight scene). It listens to `QuestionResolved` and pays out:

| Answer | Action Points | Flow |
|---|---|---|
| Correct, timer bar still **green** | +1 | +25 |
| Correct, bar already yellow or red | +1 | — |
| Wrong / TimedOut | — | — |

Two things worth knowing before changing anything here:

- **The green zone is load-bearing now.** The bridge reads its threshold
  from `QuizView.FastZoneNormalized` rather than keeping its own copy, so
  moving `fastZoneNormalized` moves what the fighter pays for. That is
  deliberate — a bar the player watched stay green that then paid nothing
  would be a lie — but it means the colour is no longer only cosmetic.
- **`QuestionResolved` is what pays out**, not `QuestionAnswered`. Anything
  that delays or suppresses the resolve event delays the reward. The
  feedback delay deliberately sits *after* it.

The quiz still has no reference to the fighter, and none of this is
required to run the quiz on its own: rewards go through a static seam that
drops the call when no fighter is listening.

Other systems subscribe alongside `QuizFlow` without interfering with it:

```csharp
using ThinkFast.Quiz;
using UnityEngine;

public class FlowMeter : MonoBehaviour
{
    [SerializeField] private QuizController quiz;

    private void OnEnable()  => quiz.QuestionResolved += OnQuizResolved;
    private void OnDisable() => quiz.QuestionResolved -= OnQuizResolved;

    private void OnQuizResolved(QuizResult result, float speed)
    {
        switch (result)
        {
            case QuizResult.Correct:  /* add Flow scaled by speed */ break;
            case QuizResult.Wrong:    /* lose a life */ break;
            case QuizResult.TimedOut: /* they didn't think fast */ break;
        }
    }
}
```

Subscribers run in subscription order, in the same frame the event fires.
`QuizFlow` shows the next question immediately when its `QuestionAnswered`
handler runs, so handle results inside the events — by the time the frame
renders after `QuestionAnswered`, the panel already displays the next
question.

## Restyling the panel

`QuizPanel.prefab` is a normal prefab — edit colors, fonts, spacing, and
layout directly in the editor.

One thing to know first: in the fight scene the panel shares the screen
with the fighter and gets 25 % of the width, so `SplitScreenLayout`
**scales it down uniformly** (to about 0.675) rather than re-flowing it to
a narrower `RectTransform`. That means the design keeps working at any
split ratio and nothing wraps that did not wrap at the authored width —
but it also means the panel is laid out at **640 wide, always**. Style it
against that width. If you change it, update the layout's
`quizPanelWidth` to match, or the scale will be computed against the wrong
number. The per-state button colors (normal /
correct / wrong / highlight) are inspector fields on each `AnswerButton`.

Only regenerate the prefab (**Tools > Quiz > Create Quiz Panel**) if you
want to reset it: regeneration **overwrites your styling**.

## Tools menu reference

- **Tools > Quiz > Create Quiz Panel** — (re)generates `QuizPanel.prefab`
  and imports TMP essentials if missing.
- **Tools > Quiz > Add Quiz Panel To Sample Scene** — puts the panel, an
  EventSystem, and the sample question into `SampleScene`. Safe to re-run.
- **Tools > Quiz > Update Sample Question (Pac-Man image, 5s)** —
  regenerates the procedural Pac-Man sprite and points the sample question
  at it.
- **Tools > Quiz > Add Quiz Flow To Sample Scene** — adds the looping
  `QuizFlow` with the sample question, sets authored/math weights,
  and clears the controller's starting question. Safe to re-run.
- **Tools > Quiz > Add Place Questions To Sample Scene** — creates
  `Landmarks.asset` (curated Wikipedia titles) if missing, wires a
  `WikipediaQuestionSource` (prompt "Which place is this?") into the
  controller, and registers it in the scene's `QuizFlow` with weight 2.
  Safe to re-run.
- **Tools > Quiz > Add Animal Questions To Sample Scene** — the same, with
  `Animals.asset` and the prompt "Which animal is this?". A second
  `WikipediaQuestionSource` on the controller, registered in `QuizFlow`
  with weight 2. Safe to re-run.
- **Tools > Quiz > Restyle Top Pane / Repair Question Label Overrides** —
  one-time migration/repair commands from the panel's layout evolution;
  both are no-ops on an already-correct prefab/scene and are kept for
  reference.
- **Tools > Quiz > Add Answer Icons To Panel** — adds a disabled
  `AnswerIcon` image to each answer button in `QuizPanel.prefab`, needed to
  display shape-sequence answer images. Safe to re-run.
- **Tools > Quiz > Add Sequence Questions To Sample Scene** — sets the
  sample scene's `QuizFlow` **Sequence Weight** to 2 so number and shape
  sequence questions mix into the endless rotation. Safe to re-run.

## Credits

Place and animal images are loaded live from Wikipedia (Wikimedia
Commons). Keep an "Images: Wikipedia (Wikimedia Commons)" credit on the
itch.io page.

## Tests

The rules core is covered by EditMode tests (`Assets/Tests/EditMode/`).
Run them from **Window > General > Test Runner**, or headless (editor must
be closed):

```
"/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testResults results.xml -logFile -
```
