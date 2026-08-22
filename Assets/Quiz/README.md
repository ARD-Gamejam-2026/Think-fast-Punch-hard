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
| `QuizFlow` | Cycles through a question list, looping forever |
| `Editor/QuizPanelBuilder` | One-time generators under **Tools > Quiz** |

## Adding a new question

1. In the Project window: **right-click > Create > Quiz > Question**
   (put it in `Assets/Quiz/Questions/`).
2. Fill in the inspector:
   - **Question Text** — shown at the top.
   - **Image** — optional sprite. Leave empty and the image panel hides
     itself; assign one and it shows between question and timer.
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

    private void OnAnswered(QuizResult result)
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

`QuestionAnswered` fires after the feedback colors have been on screen for
the controller's **Feedback Delay Seconds** (default 1.5 s).

## Restyling the panel

`QuizPanel.prefab` is a normal prefab — edit colors, fonts, spacing, and
layout directly in the editor. The per-state button colors (normal /
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
  `QuizFlow` with both sample questions and clears the controller's
  starting question. Safe to re-run.

## Tests

The rules core is covered by EditMode tests (`Assets/Tests/EditMode/`).
Run them from **Window > General > Test Runner**, or headless (editor must
be closed):

```
"/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testResults results.xml -logFile -
```
