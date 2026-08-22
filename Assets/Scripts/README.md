# Fighter System

The left half of "Think fast, Punch hard": a 2D sidescroller fighter with 3D art
and 2D physics. One character, one attack button, keyboard only — the mouse
belongs to the quiz half and is never needed here.

Everything below is in `Assembly-CSharp` (no asmdef). That means this code **can**
reference the `Quiz` assembly, but the quiz **cannot** reference this — Unity only
allows `Assembly-CSharp → asmdef`, never the reverse. Integration therefore flows
quiz → fighter by us subscribing to their events.

---

## Component map

| Piece | Namespace | Role |
|---|---|---|
| `PlayerInputReader` | `ThinkFast.Player` | Reads the Input System, exposes plain values. Latches presses for FixedUpdate. |
| `PlayerController` | `ThinkFast.Player` | Movement: run, jump, air, fast fall, drop-through, ground check, facing, hitstun. |
| `PlayerAttack` | `ThinkFast.Player` | Attack state machine, hitboxes, AP payment, Flow multipliers. |
| `PlayerHitReaction` | `ThinkFast.Player` | Turns a landed hit into knockback + stun + respawn. |
| `PlayerDebugHud` | `ThinkFast.Player` | IMGUI debug readout. Not game UI. |
| `PlaceholderAttackFx` | `ThinkFast.Player` | Throwaway synthesised audio + primitive visuals. |
| `DebugSelfDamage` | `ThinkFast.Player` | Throwaway. `H` applies a canned hit. |
| `Health` | `ThinkFast.Combat` | HP only. Implements `IDamageable`. **Shared — the AI will use this.** |
| `HitInfo` / `IDamageable` | `ThinkFast.Combat` | The combat contract. |
| `AttackDefinition` | `ThinkFast.Combat` | Frame data + payload for one attack. Serializable, reusable. |
| `TrainingDummy` | `ThinkFast.Combat` | Throwaway punching bag. **To be replaced by the AI.** |
| `FighterResources` | `ThinkFast.Economy` | Action Points + Flow + Flow state. |
| `RiddleRewards` / `IRiddleRewardSink` | `ThinkFast.Economy` | The seam to the quiz half. |
| `DebugRiddleDriver` | `ThinkFast.Economy` | Throwaway stand-in for the quiz. |
| `PlaceholderFlowStateVisual` | `ThinkFast.Economy` | Throwaway gold tint + orbiting motes. |
| `FollowCamera` | `ThinkFast.CameraRig` | Dead zone + smoothing + look-ahead + bounds. |
| `FighterHud` | `ThinkFast.UI` | Real uGUI HUD: health, AP pips, Flow bar. |
| `SplitScreenTodoAttribute` | `ThinkFast.Common` | Marks settings that split screen will invalidate. |

Anything named `Debug*` or `Placeholder*` is **deliberately throwaway** and safe to
delete once the real thing exists.

---

## Scene setup

Nothing is hand-placed. Two generators under **Tools > Think Fast**:

| Menu item | Builds |
|---|---|
| `Build PlayerController Test Scene` | Stage, one-way platforms, player, dummy, camera — into `Assets/Scenes/PlayerControllerTest.unity` |
| `Build Fighter HUD` | The uGUI canvas, wired to find the player at runtime |

Both are idempotent. The test-scene builder destroys and rebuilds everything under
a single root, so **re-running resets any Inspector tuning**. The HUD is a separate
root and survives a test-scene rebuild.

Re-run the scene builder whenever a component is added to the player in code.

---

## How it fits together

```
Input System ──► PlayerInputReader ──► PlayerController ──► Rigidbody2D
                        │                    ▲
                        │                    │ MoveControlScale, FacingLocked,
                        ▼                    │ ApplyStun
                  PlayerAttack ──────────────┘
                        │
                        │ Physics2D.OverlapBox during Active frames
                        ▼
                   IDamageable ──► Health ──► PlayerHitReaction ──► knockback + stun
                        │             │
                        │             └──► FighterHud (polls)
                        │
   FighterResources ────┘  pays 1 AP per swing, multiplies damage + knockback
        ▲
        │ GrantSolve(flowReward)
   RiddleRewards  ◄──── quiz half (or DebugRiddleDriver)
```

**Key rule:** presentation never sits in gameplay code. `PlayerAttack` raises
`AttackBecameActive`, `HitLanded` and `AttackRefused`; `FighterResources` raises
`FlowStateEntered`/`Exited`. The placeholder FX only *listen*. Deleting every
`Placeholder*` file changes no gameplay.

---

## Core contracts

### `IDamageable` / `HitInfo`

The only thing an attacker needs to know about a target.

```csharp
public interface IDamageable
{
    void TakeHit(in HitInfo hit);
}

public readonly struct HitInfo
{
    public readonly int Damage;
    public readonly Vector2 Knockback;   // world space, already flipped for attacker facing
    public readonly float Hitstun;       // seconds
    public readonly GameObject Source;   // who threw it
}
```

Knockback arrives **already resolved into world space** — the receiver never needs
to know which way the attacker was facing.

### `RiddleRewards` — the quiz seam

```csharp
RiddleRewards.GrantSolve(flowReward);   // one line, no scene wiring
```

`FighterResources` registers itself as the sink in `OnEnable`. Calls are dropped
harmlessly when no fighter exists, so the quiz half runs standalone.

The rule "no Flow while Flow state is active" lives **here**, not at the call site,
so callers cannot get it wrong.

---

## The fighter in detail

### Movement (`PlayerController`)

Dynamic `Rigidbody2D` with velocity driven manually. Unity 6 naming: `linearVelocity`,
not `velocity`.

Non-obvious behaviour, all of it load-bearing:

- **Jump is authorised by buffer + coyote**, not by `IsGrounded`. That indirection *is*
  the game feel.
- **Rising disqualifies grounded.** Without it, jumping up through a one-way platform
  briefly reports grounded and grants a free mid-air jump.
- **`jumpGroundLockout` (0.08s)** ignores ground right after launch, for the same reason.
- **Drop-through** disables collision per collider pair (`Physics2D.IgnoreCollision`),
  because rotating a `PlatformEffector2D` would let *everything* through. `OverlapBox`
  does not respect `IgnoreCollision`, so dropped platforms are filtered out of the
  ground check by hand.
- **Collision is restored only once genuinely clear** of the platform, else the solver
  ejects you back on top. 1.5s hard timeout as a backstop.
- **Hitstun skips horizontal movement entirely** rather than decelerating — decelerating
  would kill the knockback that is meant to be carrying you. Input is drained, not
  ignored, so nothing fires the instant stun ends.

Public surface used by other components:

```csharp
bool IsGrounded, HasCoyote, HasBufferedJump, IsFastFalling,
     IsDroppingThroughPlatform, IsStunned
float Facing                  // -1 or +1, never 0
Vector2 Velocity
float MoveControlScale { get; set; }   // 0..1, set by PlayerAttack each step
bool FacingLocked { get; set; }
void ApplyStun(float duration)         // extends, never shortens
```

### Attack (`PlayerAttack`)

`Ready → Startup → Active → Recovery`, driven in `FixedUpdate`.

- **Grounded is sampled once**, when the swing starts. Landing mid-punch does not
  switch you to the other attack.
- **Movement lock is per-phase.** Startup and Active root you (the commitment);
  Recovery only prevents *attacking*, not walking — otherwise you cannot chase what
  you just knocked away.
- **Facing locks during Startup/Active only**, so the hitbox cannot be flipped mid-swing.
- **One hit per target per swing**, tracked in a `HashSet<IDamageable>`.
- **AP is paid at swing start.** No AP → no swing, `AttackRefused` fires, and the
  buffered press is consumed (otherwise the refusal cue fires every physics step).
- **Being hit cancels the swing and does not refund the AP.**

### Economy (`FighterResources`)

- 1 AP per solve, 1 AP per attack, hard cap (default 5). The cap is the anti-farm rule.
- Flow drains constantly — it is a gauge, not a bank.
- At 100 → Flow state: damage and knockback multiply, drain accelerates, **and Flow can
  no longer be added**. The burst cannot be extended; the only way to spend it is to
  fight. AP still accrues.
- Drains to 0 → state ends, gauge rebuilds from zero.

### Health (`Health` + `PlayerHitReaction`)

Deliberately split. `Health` is only a number plus events; how a body reacts to being
hit is specific to that body. **The AI opponent shares `Health` and writes its own
reaction.**

0.15s invulnerability after each hit stops one lingering hitbox draining the bar.

---

## Controls

| Key | |
|---|---|
| `A` / `D` (or arrows) | move |
| `S` (or down arrow) | fast fall in air, drop through a one-way platform on the ground |
| `W` / `Up` | jump (hold = higher) |
| `Space` | attack — ground or air variant |

Bindings live in `Assets/Settings/Input/FighterControls.inputactions`, map `Fighter`.
`Move` deliberately has **no "up"** — up is jump, and binding `W` to both would leave
`Move.y` stuck at 1 for the whole jump.

### Debug keys (throwaway)

| Key | |
|---|---|
| `1` / `2` | simulate a slow / fast quiz solve (+1 AP, +8 / +25 Flow) |
| `3` | toggle auto-solve |
| `0` | reset AP and Flow |
| `H` | take a canned hit (knocked backwards relative to facing) |
| `R` | reset the training dummy to its spawn point |

---

## Tuning defaults

All are `[SerializeField]` with tooltips. **Every value is provisional** — animation
length will dictate the real timings, so do not over-tune before art lands.

| Movement | | Jump | | Attack (ground / air) | |
|---|---|---|---|---|---|
| max run speed | 9 | jump height | 3.2 | startup | 0.15 / 0.08 |
| ground accel | 90 | jump cut | 0.45 | active | 0.06 / 0.06 |
| ground decel | 110 | coyote time | 0.10 | recovery | 0.18 / 0.12 |
| turn multiplier | 2.0 | jump buffer | 0.12 | damage | 10 / 7 |
| gravity scale | 5 | ground lockout | 0.08 | knockback | (11,4) / (7,5) |
| fall multiplier | 1.4 | fast fall gravity | 3.2 | hitstun | 0.40 / 0.25 |
| max fall speed | 24 | fast fall max | 34 | move control | 0 / 0.6 |

| Economy | | Health | |
|---|---|---|---|
| max AP | 5 | max health | 100 |
| starting AP | 2 | invulnerability | 0.15 |
| max Flow | 100 | respawn delay | 1.5 |
| Flow drain | 6 /s | | |
| Flow-state drain | 20 /s (→ 5s window) | | |
| damage multiplier | ×2 | knockback multiplier | ×1.6 |

---

## Building the AI opponent

The intended next piece. What you can reuse, and what you cannot.

### Reuse directly

- **`Health`** — already generic, implements `IDamageable`. Put it on the enemy.
- **`AttackDefinition`** — plain serializable frame data, no player coupling.
- **`HitInfo` / `IDamageable`** — the enemy hitting the player already works: `Health`
  is on the player root and accepts any `HitInfo`.

### Cannot reuse as-is

- **`PlayerController`** is `[RequireComponent(typeof(PlayerInputReader))]` and reads
  input directly. An AI needs either its own simpler motor, or the movement maths
  extracted behind an input-agnostic interface (a small struct of
  `moveX / jumpHeld / jumpPressed` would do it).
- **`PlayerAttack`** likewise owns the state machine *and* reads input *and* pays AP.
  The startup/active/recovery runner is worth extracting so both fighters share one
  implementation — otherwise frame-data bugs must be fixed twice.

### Replace

- **`TrainingDummy`** duplicates HP, knockback and hitstun rather than using `Health`.
  It predates `Health` and should be deleted once the AI exists. Its hit-flash via
  `MaterialPropertyBlock` is worth keeping — copy that into the enemy's hit reaction.

### Gotchas

- **No physics layers are in use.** Everything is on `Default`. Hitboxes skip only the
  attacker's own hierarchy (`transform.IsChildOf`). Once there are two fighters that
  is still fine, but any friendly-fire or projectile rules will need real layers.
- **Flow multipliers are applied by the attacker**, reading its own `FighterResources`.
  An enemy with no `FighterResources` simply hits at ×1 — no null handling needed.
- **`PlayerHitReaction` respawns on death.** There is no round flow, no win condition,
  no score. That is still an open design question (see `GAME_DESIGN.md`).
- The enemy must not be able to hit itself: mirror the `IsChildOf` self-skip.

### Suggested split

1. Enemy that exists, has `Health`, takes hits, gets knocked back — replaces the dummy.
2. Enemy that moves: chase / back off, reusing or sharing the motor.
3. Enemy that attacks, using shared frame-data logic.

---

## Tooling

### Headless compile check

```
powershell -NoProfile -ExecutionPolicy Bypass -File tools/compile-check.ps1
```

Unity allows only one instance per project, so `Unity.exe -batchmode` **fails while the
editor is open**. This rebuilds Unity's generated csproj files with a glob over
`Assets/Scripts` instead, via `dotnet` — about 2 seconds, no Unity needed.

It verifies code **compiles**, not that it **runs**. Editor scripts in particular fail
at runtime the moment an Inspector draws. After any editor-side change, also check
`Logs/Editor.log` — it carries full stack traces and is readable without Unity.

It also lints for `UnityEditor` usage in runtime code, which the compiler cannot catch
here (inside the editor, `Assembly-CSharp` references `UnityEditor` too, so it compiles
and only breaks at player build time).

### `[SplitScreenTodo]`

Marks settings tuned against a full-screen 16:9 view that split screen will invalidate.
Renders as an info box above the field in the Inspector.

```
grep -rn "SplitScreenTodo(" Assets/Scripts
```

---

## Known gaps

- **No opponent.** `TrainingDummy` is a static bag; the fight is one-sided.
- **No round flow.** Death respawns after 1.5s. No win condition, score, or reset.
- **Quiz not wired.** `RiddleRewards` exists and is driven only by `DebugRiddleDriver`.
  See below.
- **No tests.** The economy is pure logic and would test well, but test assemblies
  cannot reference `Assembly-CSharp` — testing it requires moving this code into an
  asmdef first (which is exactly why the quiz has one).
- **No text in the HUD.** TextMeshPro essentials are not in the project yet; they
  arrive with the menu branch. Importing a second copy would collide with it.
- **`CLAUDE.md` is stale** — it still says gameplay code does not exist and no asmdefs
  are used. Both untrue. It is on `main`, which this branch has not merged yet.

## Wiring the quiz half (ready to do)

`QuizController` (on branch `marceltov/quizzes`) raises two events, both carrying
result plus answer speed as normalized time remaining (1 = instant, 0 = timed out):

- **`QuestionResolved`** — fires instantly on click/timeout. **Use this one for rewards.**
- `QuestionAnswered` — fires after the feedback delay; `QuizFlow` uses it to advance.

The whole adapter:

```csharp
using ThinkFast.Economy;
using ThinkFast.Quiz;

void OnQuestionResolved(QuizResult result, float speed)
{
    if (result != QuizResult.Correct) return;
    RiddleRewards.GrantSolve(Mathf.Lerp(minFlowPerSolve, maxFlowPerSolve, speed));
}
```

Question sequencing is already handled by their `QuizFlow` — an endless stream that
auto-advances on every resolution including timeouts, which is the auto-reset timer
the design calls for. See `Assets/Quiz/README.md` on that branch.
