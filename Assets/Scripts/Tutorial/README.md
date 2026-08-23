# Tutorial

A third scene next to the fight and the menu, reachable from the menu's
**TUTORIAL** card. It teaches both halves of the game against a trainer that
never hits back: move, jump, punch, run out of Action Points, answer a question
to get them back, answer *fast* to build Flow, and finally what winning looks
like.

Everything in `Assets/Scenes/Scene_Tutorial.unity` is generated. There is
nothing hand-placed in it, so the scene is rebuilt from scratch every time
rather than opened and patched:

```
Tools > Think Fast > Build Tutorial Scene
```

That command also adds the scene to Build Settings and creates the tutorial's
question assets if they are missing. Re-running it is always safe. The menu's
card is built separately — after adding the scene, re-run **Tools > Think Fast
> Build Menu UI** so the third tile appears.

---

## Component map

| Piece | Role |
|---|---|
| `TutorialDirector` | The state machine. Watches the real systems and decides when a step is done. |
| `TutorialScript` | **What the tutorial says**, all ten steps in order. Content, no behaviour. |
| `TutorialStepDefinition` | One card: title, body, objective, and what it is waiting for. Plain data. |
| `TutorialGoal` | The nine things a step can wait for. |
| `TutorialCoach` | The card on screen. Knows no tutorial content — every string is handed to it. |
| `TutorialDummy` | Makes the opponent a punching bag: stands still, turns to face you, gets back up. |
| `Editor/TutorialSceneBuilder` | Builds the scene. |
| `Editor/TutorialQuestions` | Creates the eight easy questions the tutorial asks. |
| `Editor/TutorialTextFitCheck` | Measures every step's copy against the label it has to fit in. |

## Re-wording a step

The copy lives in `TutorialScript.Build()` — one table, top to bottom, nothing else to
read. **Keep each body to about two lines** (roughly 145 characters at the authored
size), then run:

```
Tools > Think Fast > Check Tutorial Text Fits
```

It measures every title, body and objective against its actual label with the actual
font and reports anything that overruns. Bodies and objectives auto-shrink when they
overflow; a title that overruns is **clipped**, which is why the check calls those out
separately. It caught exactly that on the first run — every title was 3 px short of a
single line.

## How it teaches

It watches, it does not script. Every goal is read off the components the real
fight already uses — `PlayerController`, `PlayerAttack`, `FighterResources`,
`QuizController` — so the tutorial cannot claim the game does something it does
not. The fast-answer threshold in particular is read off `QuizView`, the same
place `QuizRewardBridge` reads it, so "answer in the green" means exactly what
the bar shows and exactly what pays out.

Three things it *does* reach in and change:

- **The quiz stays shut** until step 6. `QuizFlow` is built disabled and the
  panel inactive; a component's `Start` runs the first time it is enabled, so
  that one flag is also what holds the first question back. A countdown ticking
  behind the movement lesson would be noise.
- **The punching step tops your points up.** After 1.2 s at zero AP it hands one
  back, but only while the step says "land two hits". Without it a new player
  who whiffs twice is stuck in a step they can no longer finish.
- **The out-of-points step empties your points**, by calling the same
  `TryPayForAttack` the attack does. It has to actually be true that you have
  none, or the lesson is a lie.

## The steps

The script lives in one method, `TutorialDirector.BuildSteps()`. It is the part
most likely to be re-worded and it should never require reading any of the code
around it — so the wording, the objective and the goal all sit together, in
order, and nothing else in the file needs touching to change what the player is
told.

Two steps carry `CanBeSkipped`: **out of points** and **Flow state**. Flow state
needs several fast solves in a row and a tutorial must never become the thing
the player is stuck inside, so those two offer a button past them. The rest are
gated on their goal, and the corner **SKIP TUTORIAL** button jumps to the last
card, which is where the exits are.

`MinimumSeconds` keeps a card up for a beat even when its goal is already met —
without it the Flow-state card flashes past unread for anyone who happened to be
in Flow state when it appeared.

## The look

Soft-console UI, the same family as the menu: one white rounded card floating on a
soft shade, generous padding, one accent, and dark grey type rather than black.

Three things were wrong with the first pass and are worth not undoing:

- **`MenuButton` used to hard-code its own colours** — every button in the game was
  `Lerp(Surface, AccentWash)` with `Lerp(TextPrimary, Accent)` type. That is why the
  screens read as untouched default UI: nothing could ever be emphasised. It now takes
  four serialized colours, defaulting to exactly those values, so every existing screen
  is unchanged and a builder can opt into a filled button. `UiFactory.PillStyle` picks
  between `Quiet`, `Subdued` and `Primary` — **one `Primary` per screen**, or nothing is
  emphasised again.
- **Accent-coloured text was 2.8:1.** `MenuTheme.Accent` is a fill colour; it cannot
  carry type. `AccentInk` (7.1:1) is the same idea dark enough to read, `AccentStrong`
  (4.1:1 under white) is the fill for the primary button, and `TextSecondary` (5.7:1)
  replaces `TextMuted` (3.0:1) for anything that is a sentence rather than a caption.
  Every text pairing on the card now clears WCAG AA.
- **The card was translucent** (`MenuTheme.HudCard`, 94% white) over a lit 3D stage, so
  the scene showed through behind every letter. It is opaque now, and the shade behind
  it is what separates it from the stage instead.

The objective and its progress bar are **one element**: a pill that fills as you get
closer to finishing the step. Two elements cost a whole row of a card that has none
spare, and split "what to do" from "how far in you are" into two things to look at.

## Scene layout

Four generated roots, in the order the builder makes them:

1. `--- Tutorial Stage (generated) ---` — floor, two walls, one ledge, the
   fighter prefabs. Blocks and platforms come from `FighterStageParts`, shared
   with the fight's rig builder so the one-way platform behaves identically.
2. `--- Quiz (generated) ---` — the same `QuizPanel` prefab the fight uses, a
   `QuizFlow` restricted to the tutorial's own authored questions, and the
   normal `QuizRewardBridge`.
3. `--- Split Screen (generated) ---` — the same 75/25 split, plus the placard
   that stands in for the quiz until it opens.
4. `--- Tutorial (generated) ---` — the director, the screen fade and the
   menu's button sounds.

Plus the fighter HUD, built by the fight's own `FighterHudBuilder`.

**Two ordering rules the builder depends on**, both commented at the call site:
the split-screen layout must exist before the HUD, which registers itself with
it; and the HUD must exist before the coach card, which is parented *inside*
the HUD's viewport rect. That parenting is what confines the card to the
fight's half of the window without this builder ever naming the split ratio.

## Things worth knowing before changing it

- **The tutorial owns no round.** There is no `RoundFlow`, so nothing can end
  and nothing loads the end screen. `TutorialDummy` revives inside
  `EnemyKnockout`'s own report delay, so a round-ended report is never made —
  if you raise `reviveDelaySeconds` above that delay, the tutorial will start
  trying to end a round it is not running.
- **Tutorial questions live in `Assets/Quiz/TutorialQuestions/`**, not
  `Assets/Quiz/Questions/`. The fight loads every authored question under the
  latter, and "What is 7 + 5?" turning up mid-match would be an easy thing to
  ship by accident.
- **Nothing selects a button on load, deliberately.** The UI module's submit
  action includes Space, which is also the punch — so a selected button would
  be pressed again every time the player throws a punch. The director clears
  the selection after every click for the same reason. Enter is offered as the
  keyboard shortcut instead.
- The debug components the fighter prefabs carry — the IMGUI readouts, the
  self-damage key, the stand-in riddle driver that hands out free Flow — are
  all switched off on the tutorial's instances. A tutorial is the one screen
  where they are actively misleading.
