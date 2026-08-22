# Fighter System

The left-hand side of "Think fast, Punch hard" — now literally so: a 2D sidescroller
fighter with 3D art and 2D physics, in its own viewport with the quiz beside it. One
character, one attack button, keyboard only — the mouse belongs to the quiz half and
is never needed here. Facing it is an autonomous opponent that chases, jumps and
punches back.

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
| `PlayerKnockout` | `ThinkFast.Player` | Death → stop fighting → report the round lost. |
| `PlayerDebugHud` | `ThinkFast.Player` | IMGUI debug readout. Not game UI. |
| `PlaceholderAttackFx` | `ThinkFast.Player` | Throwaway. Warm colours, hard shapes, synthesised audio. |
| `DebugSelfDamage` | `ThinkFast.Player` | Throwaway. `H` applies a canned hit. |
| `EnemyMotor` | `ThinkFast.Enemy` | Opponent movement: run, jump, gravity, ground check, facing, hitstun. Input-agnostic. |
| `EnemyBrain` | `ThinkFast.Enemy` | Every decision the opponent makes. Writes to the motor and the attack, touches no physics. |
| `EnemyAttack` | `ThinkFast.Enemy` | Ground + air attack, no AP cost. Thin wrapper over `AttackRunner`. |
| `EnemyKnockout` | `ThinkFast.Enemy` | Death → stop fighting → report the round over. |
| `EnemyDebugHud` | `ThinkFast.Enemy` | Throwaway. Floating HP + AI state label, and `R` to reset. |
| `EnemyTuningCheck` | `ThinkFast.Enemy` | Warns on Play when the tuning values contradict each other. Editor / dev builds only. |
| `PlaceholderEnemyAttackFx` | `ThinkFast.Enemy` | Throwaway. Violet, rounded shapes, low audio — and the **wind-up telegraph**. |
| `Health` | `ThinkFast.Combat` | HP only. Implements `IDamageable`. **Shared by both fighters.** |
| `IFighterMotor` | `ThinkFast.Combat` | **Shared.** What a fighter's body exposes to third parties. |
| `IFighterKnockout` | `ThinkFast.Combat` | **Shared.** A fighter being knocked out, and revived. |
| `FighterHitReaction` | `ThinkFast.Combat` | **Shared.** Knockback + stun + optional flash, for either fighter. |
| `HitInfo` / `IDamageable` | `ThinkFast.Combat` | The combat contract. |
| `AttackDefinition` | `ThinkFast.Combat` | Frame data + payload for one attack. Serializable, reusable. |
| `AttackRunner` | `ThinkFast.Combat` | **Shared.** The startup→active→recovery machine and the hitbox sweep. Plain C#. |
| `OneWayDropThrough` | `ThinkFast.Combat` | **Shared.** Falling through one-way platforms, and the restore rules. Plain C#. |
| `RendererTint` | `ThinkFast.Combat` | **Shared.** Flat-colour flash via `MaterialPropertyBlock`. Plain C#. |
| `RoundEvents` / `RoundOutcome` | `ThinkFast.Rounds` | The seam between a K.O. and the round ending. |
| `RoundFlow` | `ThinkFast.Rounds` | Records the outcome, loads the end screen — and reopens the round when a fight starts. |
| `MatchResult` | `ThinkFast.Rounds` | Carries the outcome across the scene load. One enum and a flag. |
| `EndScreen` | `ThinkFast.Rounds` | Writes which ending it was into the end scene's label. |
| `DebugRoundBanner` | `ThinkFast.Rounds` | Throwaway "K.O. — YOU WIN" banner + `Enter` to restart. Switched off once the end screen exists. |
| `FighterResources` | `ThinkFast.Economy` | Action Points + Flow + Flow state. **Player only** — it is also what identifies the player. |
| `RiddleRewards` / `IRiddleRewardSink` | `ThinkFast.Economy` | The seam to the quiz half. |
| `QuizRewardBridge` | `ThinkFast.Economy` | The quiz half plugged into that seam. The only object that knows both halves exist. |
| `DebugRiddleDriver` | `ThinkFast.Economy` | Throwaway stand-in for the quiz. Switched off once the real one is wired. |
| `PlaceholderFlowStateVisual` | `ThinkFast.Economy` | Throwaway gold tint + orbiting motes. |
| `FollowCamera` | `ThinkFast.CameraRig` | Dead zone + smoothing + look-ahead + bounds. Derives the bounds and the dead zone width from how wide its viewport actually is. |
| `FighterHud` | `ThinkFast.UI` | Real uGUI HUD: health, AP pips, Flow bar. |
| `SplitScreenLayout` | `ThinkFast.UI` | Owns the split: fighter viewport left, quiz scaled into what is left, backdrop over the side no camera clears. |
| `SplitScreenTodoAttribute` | `ThinkFast.Common` | Marks settings that split screen will invalidate. Nothing carries it now — see **Split screen** below. |
| `PlaceholderFxKit` / `PlaceholderFxShape` | `ThinkFast.Common` | Throwaway. Runtime-synthesised clips, unlit materials, self-animating primitives. Shared by both FX components. |

Anything named `Debug*` or `Placeholder*` is **deliberately throwaway** and safe to
delete once the real thing exists.

---

## Scene setup

Nothing is hand-placed. Four generators under **Tools > Think Fast**:

| Menu item | Builds |
|---|---|
| `Build PlayerController Test Scene` | Stage, one-way platforms, player, opponent, round banner, camera — into `Assets/Scenes/PlayerControllerTest.unity` |
| `Build Fighter HUD` | The uGUI canvas, wired to find the player at runtime |
| `Build Split Screen Fight` | The quiz panel, its endless flow, the reward bridge, the backdrop and the split itself — into the same scene |
| `Build Round Flow` | The end-of-round transition, plus the component that tells the end screen which ending it was — into the fight scene **and** `Scene_End` |

All four are idempotent, and each owns its own root, so one can be rebuilt without
disturbing the others. The test-scene builder destroys and rebuilds everything under
its root, so **re-running it resets any Inspector tuning** — but it leaves the HUD,
the quiz and the round flow alone.

`Build Round Flow` is the only one that touches a scene it did not create. It adds
nothing to `Scene_End`'s layout — it wires a component to the label already there, so
the end screen stays the authored design.

`Build Split Screen Fight` also switches off `DebugRiddleDriver` on the player, since
the real quiz is now paying into the same economy. Re-tick it to get the `1`/`2`/`3`
solve keys back.

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
                  AttackRunner ◄──────────────────── EnemyAttack
                        │  shared: frame data,             ▲
                        │  hitbox, self-hit skip           │ TryAttack()
                        │                                  │
                        │ Physics2D.OverlapBox        EnemyBrain ──► EnemyMotor
                        │ during Active frames             │  MoveX,      │
                        ▼                                  │  RequestJump │
                   IDamageable ──► Health ──┬──► FighterHitReaction ◄─────┘
                                     │      │      one component, either fighter,
                                     │      │      via IFighterMotor
                                     │      │
                                     │      └──► PlayerKnockout ─┐
                                     │           EnemyKnockout ──┴► RoundEvents ──► DebugRoundBanner
                                     │
                                     └──► FighterHud (polls the PLAYER's Health)

   FighterResources    pays 1 AP per swing, multiplies damage + knockback (player only)
        ▲
        │ GrantSolve(flowReward)
   RiddleRewards  ◄──── QuizRewardBridge ◄──── QuizController.QuestionResolved
                          +1 AP per correct answer,        (the quiz half)
                          Flow only if it was fast
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

### `IFighterMotor` — the shared body contract

```csharp
bool IsGrounded, IsStunned;
Vector2 Velocity;
float Facing;                       // -1 or +1, never 0
float JumpApexHeight;               // what makes reachability answerable
float MoveControlScale { get; set; }
bool FacingLocked { get; set; }
void ApplyStun(float duration);
```

Implemented by `PlayerController` and `EnemyMotor`. Both are far bigger than this —
one reads input and owns fast-fall and drop-through, the other exposes jump-distance
maths for route planning. **Only what a third party needs belongs here.**

The value is not that the two classes happen to share these members; it is that code
can be written against *a fighter* without knowing which one it has.
`FighterHitReaction` is the proof — one component knocks either fighter around,
because it never asks whose body it is on. It replaced a near-identical pair of
components, and the duplication is gone rather than merely documented.

It is deliberately **not** `IFighter`. Health, attacks and knockouts are separate
components with separate contracts; folding them in would produce an interface nothing
could implement without becoming everything.

`IFighterKnockout` is the same idea, smaller: `KnockedOut`, `Revived`, `IsKnockedOut`,
so presentation can react to a knockout without caring whose it was.

Two Unity caveats worth knowing before leaning on these further:

- **Interfaces cannot be `RequireComponent`ed**, so `FighterHitReaction` checks for one
  in `Awake` and disables itself with an error instead.
- **Interfaces cannot be serialized into Inspector fields.** `GetComponent<IFighterMotor>()`
  works fine; dragging one into a slot does not.

---

### `RoundEvents` — the match-flow seam

```csharp
RoundEvents.ReportRoundEnded(RoundOutcome.PlayerWon);   // raised by EnemyKnockout
RoundEvents.RoundEnded += outcome => { ... };           // subscribed by whatever ends the round
```

Same shape as `RiddleRewards`, for the same reason: whoever lands the killing blow
should not need a scene reference to a match manager.

**The first report wins** and later ones are dropped. A double K.O. in one physics
step must not fire two contradictory endings, and a corpse can plausibly be hit
again. `ResetRound()` reopens it.

The subscriber is now `RoundFlow`, which loads the end screen. That went in without
touching a line of combat code, which is what the seam was for. See **Round flow**
below.

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

### Attack (`AttackRunner` + `PlayerAttack`)

`Ready → Startup → Active → Recovery`, driven in `FixedUpdate`. The machine itself
is `AttackRunner` — plain C#, **shared with the opponent**, so frame data means the
same thing for both fighters and a bug is fixed once.

In `AttackRunner` (both fighters):

- **Movement lock is per-phase.** Startup and Active root you (the commitment);
  Recovery only prevents *attacking*, not walking — otherwise you cannot chase what
  you just knocked away.
- **Facing locks during Startup/Active only**, so the hitbox cannot be flipped mid-swing.
- **One hit per target per swing**, tracked in a `HashSet<IDamageable>`.
- **Self-hits are skipped** by `transform.IsChildOf(owner)`. No physics layers are in
  use, so the hierarchy is the only thing separating attacker from target.
- **A swing does not lose a step to the step it was started in.** `Begin` sets the
  phase, the *next* `Tick` starts the clock. Startup is the window an opponent gets
  to react in and is the most sensitive number in the attack.
- **`DamageMultiplier`/`KnockbackMultiplier`** are set by the owner. A fighter with
  no Flow economy just leaves them at 1 — no null handling needed.

In `PlayerAttack` (the player-specific half):

- **Grounded is sampled once**, when the swing starts. Landing mid-punch does not
  switch you to the other attack. (`EnemyAttack` does the same.)
- **AP is paid at swing start.** No AP → no swing, `AttackRefused` fires, and the
  buffered press is consumed (otherwise the refusal cue fires every physics step).
- **Being hit cancels the swing and does not refund the AP.**

### Economy (`FighterResources`)

- 1 AP per solve, 1 AP per attack, hard cap (default 5). The cap is the anti-farm rule.
- Flow drains constantly — it is a gauge, not a bank.
- At 100 → Flow state: damage and knockback multiply, drain accelerates, **and Flow can
  no longer be added**. The burst cannot be extended; AP still accrues.
- **Every swing during Flow state burns Flow on top of the drain** (`flowStateCostPerAttack`,
  default 10). This is what makes the burst something you *spend* rather than something
  you sit inside: using it is what ends it, so how long the window lasts is a decision
  rather than a timer. Charged at swing start, alongside the AP.
- Drains to 0 → state ends, gauge rebuilds from zero. A swing that empties the gauge
  lands at ×1 — the multiplier is re-read when the hit connects, and the Flow really
  did run out mid-punch.

**`TryPayForAttack()` charges both costs.** They live behind one call because a caller
must not be able to pay one and forget the other.

### Health (`Health` + `FighterHitReaction` + knockouts)

Deliberately split three ways:

- `Health` is a number plus events, shared by both fighters.
- `FighterHitReaction` turns a hit into knockback and stun, and optionally a flash.
  **One component for both fighters** — see `IFighterMotor` below.
- `PlayerKnockout` / `EnemyKnockout` decide what a death means for the *round*. Losing
  is not a property of a body, so it does not live in the hit reaction. These stay
  separate because they genuinely differ: different components to switch off, opposite
  outcomes to report.

0.15s invulnerability after each hit stops one lingering hitbox draining the bar.

**Neither fighter respawns.** Death disables the parts that fight — input and attack
for the player, brain and attack for the opponent — while leaving the motor on so the
body still falls and takes its final knockback. `Revive()` on either is a debug
affordance, not round flow.

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
| | the four above need `DebugRiddleDriver` re-ticked — the real quiz replaced it |
| `H` | take a canned hit (knocked backwards relative to facing) |
| `R` | put the opponent back on its feet at its spawn point, at full health |
| `Enter` | after either K.O., reload the scene and fight again — needs `DebugRoundBanner` re-ticked, the end screen replaced it |

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

Opponent (`EnemyMotor`, `EnemyBrain`, `EnemyAttack`):

| Movement | | Behaviour | | Attack (ground / air) | |
|---|---|---|---|---|---|
| max run speed | 6.5 | preferred distance | 1.4 | startup | 0.20 / 0.09 |
| ground accel | 60 | spacing tolerance | 0.6 | active | 0.07 / 0.10 |
| ground decel | 80 | attack range | 1.7 | recovery | 0.24 / 0.18 |
| jump height | 3.8 | attack vertical range | 1.3 | damage | 8 / 6 |
| jump buffer | 0.15 | attack reaction (ground) | 0.25 | knockback | (9,3.5) / (6,4.5) |
| | | attack reaction (air) | 0.12 | | |
| gravity / fall mult | 5 / 1.4 | attack cooldown | 1.2–2.2 | hitstun | 0.35 / 0.25 |
| | | jump reaction time | 0.4 | move control | 0 / 0.9 |
| | | drop reaction time | 0.45 | | |
| | | jump cooldown | 1.0 | | |
| | | jump reach height | 1.2 | | |
| | | drop-through height | 1.2 | | |

| Climbing | | | |
|---|---|---|---|
| jump reach margin | 0.5 (→ usable rise 3.3) | climb scan region | 24 × 10 |
| min climb rise | 0.6 | boarding tolerance | 0.4 |
| edge inset | 0.5 | rescan interval | 0.4 |
| ledge probe | 0.8 | | |

Run speed is under the player's 9 on purpose: an opponent that can always close the
gap leaves no room to kite it.

| Economy | | Health | |
|---|---|---|---|
| max AP | 5 | max health | 100 |
| starting AP | 2 | invulnerability | 0.15 |
| max Flow | 100 | round-end delay | 1.2 |
| Flow drain | 6 /s | | |
| Flow-state drain | 20 /s (→ 5s window) | | |
| Flow cost per attack | 10 (Flow state only) | | |
| damage multiplier | ×2 | knockback multiplier | ×1.6 |

---

## The opponent

Fully autonomous. No Action Points, no Flow, no input — it exists to be fought.
Three components, split so that *how it thinks* can be rewritten without any risk
to *how it moves*:

| | |
|---|---|
| `EnemyMotor` | Legs. Cannot see the player, does not know what a target is, and will happily walk into a wall forever. |
| `EnemyBrain` | Decisions. Reads the world, writes `MoveX` / `RequestJump()` / `TryAttack()`. Touches no physics. |
| `EnemyAttack` | One attack, ground and air flavours, over the shared `AttackRunner`. |

### What it does

- **Chases**, holding a preferred distance with a dead band either side — without
  the dead band it vibrates against the player capsule.
- **Jumps** when the target has *stayed* above it **and is actually within one jump**,
  or immediately when a shin-height probe finds a wall or step in the way, or when the
  floor is about to run out while it is heading somewhere higher.
- **Climbs** via an intermediate platform when the target is too high to reach directly
  (see below).
- **Drops through** the one-way platform it is standing on when the target has *stayed*
  below it.
- **Attacks** on a randomised cooldown once the target has been in reach for
  `reactionTime`. Ground or air, chosen the same way the player's is. Range is measured
  against **where the fighter will be when the hitbox opens**, not where it is — see
  below.

### Reaction windows

Every reaction to the *player* is gated on the condition holding for a moment first —
`reactionTime` for attacking, `jumpReactionTime` for following upward,
`dropReactionTime` for following downward. Reacting on the frame a condition becomes
true does not read as an opponent reacting; it reads as a **mirror**, and the mirroring
is what makes it look hectic. Making it wait means a quick hop is ignored and only
actually going somewhere gets followed.

Two things deliberately skip the windows:

- **Being blocked by geometry.** That is being stuck, not reacting to you, and
  hesitating about it just looks broken.
- **Hitstun and losing the target** *clear* every part-built window
  (`ForgetReactionWindows`). Coming out of stun with a window three-quarters full would
  produce exactly the instant reaction they exist to prevent.

Raise all three to make the opponent calmer; lower them to make it stick to you.

### Reaching height (`UpdateRoute` / `TryFindSteppingStone`)

The opponent knows what it can reach: `UsableRise` is the motor's jump apex minus a
margin. Every vertical decision is checked against it, which gives three outcomes:

| Target is… | It… | State |
|---|---|---|
| within `UsableRise` | jumps at it after the reaction window | `Chase` |
| higher, but a platform in between is reachable | goes and stands under **that**, jumps onto it, then re-plans | `Climbing` |
| higher, with nothing to climb via | **does not jump at all** — shadows the target from below | `Stranded` |

The third case is the fix for the opponent hammering the jump button under a ledge it
could never reach. It was never asking whether the jump *could work*, only whether the
target was up.

**`TryFindSteppingStone` plans exactly one hop**, not a route. It scans for surfaces
above the current standing height, discards anything that gains less than
`minClimbRise` or more than `UsableRise` or overshoots past the target, and scores what
is left. That is enough, because it **re-plans on every landing** — each hop makes the
next one reachable, so a two-platform climb emerges without anything reasoning about
the whole route.

Scoring, in order of weight:

1. **A stone that leaves the target within one more jump wins outright.** This is the
   closest thing here to real planning: it cannot see a whole route, but it can see
   whether *this* hop finishes the job, which keeps it off dead ends whenever a
   completing stone exists at all.
2. Otherwise, highest first — height is what it is short of.
3. Ties broken by how much closer the stone leaves it to standing under the target.
   This is what picks the correct side of the stage.

Three supporting rules make the hops actually land:

- **A jump is a commitment.** `UpdateRoute` returns immediately when airborne and
  re-plans *only on landing*. This is load-bearing and easy to break: height is
  measured from where the fighter currently is, so during the hop the gap to the target
  shrinks, and near the apex it drops under `UsableRise`. Re-planning there reads the
  route as "no longer needed", abandons the hop half-done, chases the target instead,
  sails past the ledge and lands back at the start — forever, because the loop is
  stable. Air control is spent getting *onto* the platform, not drifting toward the
  player.
- **Boarding spot.** It steers to a point *inset from the platform edges* and directly
  under it, then jumps straight up through the one-way collider. No reaction window —
  this is navigation, not a reaction to the player.
- **Ledge jump.** Running out of floor while heading somewhere higher triggers a jump
  immediately. This is what crosses the gap *between* two platforms; hesitating there
  just means falling back down a level.

**Navigation jumps ignore `jumpCooldown`, and they have to.** A boarding hop is
airborne for ~0.6s against a 1.0s cooldown, so the fighter lands still holding it — and
the ledge it must jump from next is ~0.5 units away, well under half a second of
walking. It would detect the ledge, be refused, walk off, fall, climb again, and repeat
forever. That loop looks like indecision but is really the cooldown outliving the
platform. Exempting them cannot cause rapid fire: both require being grounded, and any
jump costs ~0.6s in the air. The cooldown stays on jumps that are a *reaction* —
following the target upward, or hopping a wall — which is the only place repeated
firing reads as twitchiness.

The two climbs in the test stage, both verified against the actual geometry:

| From | Hop 1 (boarding jump) | Hop 2 (ledge jump) |
|---|---|---|
| floor → Platform Top Right (5.0) | via Platform Low Right (2.6), board at x 6.5 | gap 2.25 vs reach 2.87 |
| floor → Platform High Mid (4.6) | via Platform Low Left (2.6), board at x −7.25 | gap 2.75 vs reach 3.25 |

Hop 2 is launched from a **standstill** — the boarding hop lands with no horizontal
speed, and the ledge fires on the same step — so those reach figures already include
accelerating from zero at `groundAcceleration`. A running launch would clear by more.

Margins on the second hop are only ~0.5 units. **That is why the opponent jumps higher
than the player** (3.8 vs 3.2) — the apex buys the air time that carries the running
jump across the gap. At 3.4 the margin halves and the hops start failing
intermittently. See **Retuning the fighters** below before changing jump height, run
speed or gravity, and re-check these numbers if platform heights or gaps change.

### Aiming where it will be (`TryAttack` / `PredictedTravel`)

*"Is the target in reach?"* is the wrong question. The hitbox does not exist until
startup has elapsed, so the right question is whether they are in reach of **where the
fighter will be when it opens**. `PredictedTravel` answers that, and the range test runs
against the prediction rather than the present.

Both axes matter, and the vertical one is the easy one to forget:

- **Horizontally**, a jump closes most of an attack range during startup. Without the
  lead the opponent arcs past a ledge-camper and only registers them once a swing could
  no longer have landed. Travel is scaled by the attack's own `moveControlScale`,
  because the swing brakes the fighter — predicting off the current speed overshoots
  and makes it swing *too early* instead.
- **Vertically**, a fighter blocked by the target's own body loses its horizontal speed
  and simply falls. Falling at 4 u/s it drops **1.10 units during a 0.13s startup —
  exactly one hitbox height**. It aimed at the target and the hitbox opened underneath
  them. That is the near miss that makes standing on the edge of a platform safe.

The airborne reaction window is also its own, much shorter number (`airReactionTime`,
0.12s vs 0.25s): a jump arcing past the target is in range for about a third of a
second, and the jump was itself the decision — deliberating a second time mid-flight
means never swinging.

Three further changes bought the dive its remaining margin:

| | before | after | why |
|---|---|---|---|
| `EnemyBrain` execution order | default | `-10` | it ran *after* `EnemyAttack`, so a swing decided in one step was not ticked until the next — a whole physics step of latency |
| air startup | 0.13 | 0.09 | every frame of startup is spent travelling back out of range |
| air active | 0.07 | 0.10 | the only forgiveness a dive gets for arriving a step or two off |
| air `moveControlScale` | 0.6 | 0.9 | a dive that brakes itself lands short of what it was aimed at |

Decision-to-hitbox went from 0.150s to 0.090s, and the hitbox stays open 43% longer.

Camping the *far* end of a platform still gets no mid-air hit — but there the opponent
has room to land on the platform and simply fight you on it, which is the outcome you
want anyway.

### Retuning the fighters — what adapts and what does not

Most of the AI derives its behaviour from the settings rather than assuming them.
`UsableRise` comes from the motor's jump apex, swings are aimed with the attack's real
frame data and the fighter's live velocity, and jump launch speed is derived from the
desired apex so changing gravity does not change how high anyone jumps. **Change those
values and the AI re-reasons correctly.**

One thing does *not* adapt, and it is the one to watch:

> **How wide a gap the opponent can jump is emergent, and nothing checks it.**
> It falls out of jump height, run speed, gravity scale and fall multiplier
> *together*, and it has to be big enough for the gaps in the stage.

Sensitivity of the two test-stage climbs, as margin in units (negative = the hop fails
and the opponent loops between platforms instead of getting up):

| Change | usable rise | Top Right hop | High Mid hop |
|---|---|---|---|
| **current** | 3.30 | +0.62 | +0.50 |
| enemy `jumpHeight` 3.4 | 2.90 | +0.17 | +0.12 |
| enemy `jumpHeight` 3.2 | 2.70 | **−0.08** | **−0.10** |
| enemy `maxRunSpeed` 5.5 | 3.30 | +0.17 | **−0.00** |
| enemy `maxRunSpeed` 5.0 | 3.30 | **−0.05** | **−0.25** |
| `gravityScale` 7 | 3.30 | +0.17 | **−0.00** |
| `gravityScale` 3.5 | 3.30 | +1.17 | +1.13 |

Read that as: **lowering the opponent's jump height or run speed is the dangerous
direction**, and roughly a 15% cut in either is enough to break the climbs in the
current stage. Raising them, or making gravity floatier, is always safe. Player
settings do not affect this at all — only the opponent's own.

The failure is graceful, not a crash: it retries, or reports `Stranded`. But it will
never get up, and a platform becomes a safe camping spot.

### `EnemyTuningCheck` — the safety net

A component on the opponent that runs once on Play, warns about settings that have
drifted out of agreement, and compiles to nothing outside the editor and development
builds. It never corrects anything.

What it catches:

| Check | Why it matters |
|---|---|
| usable rise ≤ 0 | `jumpReachMargin` ate the whole jump; it can never climb |
| player jumps higher than the opponent can land | **any** ledge the player reaches becomes safe — currently 3.2 vs 3.30, only 0.1 of headroom |
| `preferredDistance` > `attackRange` | it holds a distance it cannot punch from, walks up and does nothing |
| `attackRange` > ground hitbox reach | the ground attack roots the fighter during startup, so it must connect from where it stood |
| stopping distance > `climbEdgeInset` | boarding jumps launch from past the platform edge and miss |

Tick `alwaysReportReach` to also print the derived figures when nothing is wrong — how
high it can climb, and how wide a gap it can cross at each height. Those are the two
numbers a stage layout has to respect, and nothing else in the project can work them
out.

### Why the swing is telegraphed

`AttackRunner.Started` fires at the beginning of startup, and
`PlaceholderEnemyAttackFx` draws a shape that **grows to the true hitbox size over
exactly the startup duration**. Startup is the longest phase of a swing, and with
nothing drawn during it the opponent appears to stop dead and then hit you — which
reads as the game hitching, not as an attack being charged.

That is why the startup could then be shortened (0.28 → 0.20) without the attack
becoming unfair: the wind-up now carries information rather than dead time. If the
telegraph is ever removed, the startup has to come back down further or the attack
becomes unreadable again.
- **Gets stunned and knocked back** like the player, and **cancels its swing** when hit.
- **On K.O.**: brain and attack switch off, the motor stays on so the body still
  falls and takes knockback, then `RoundEvents.ReportRoundEnded(PlayerWon)` fires
  after a short delay.

### The dials that matter

Four numbers decide whether it feels fair, and none of them is damage:

- **`EnemyAttack.groundAttack.startup`** (0.20s vs the player's 0.15s) — the wind-up
  *is* the window you get to react in. Only safe at this length because it is drawn.
- **`EnemyBrain.reactionTime`** (0.25s) — an opponent that swings the instant you
  enter range is unreadable no matter how weak the hit is. Its airborne twin
  `airReactionTime` (0.12s) is short on purpose; raising it back up makes ledges safe
  to camp again.
- **`EnemyBrain.jumpReactionTime`** (0.4s) and **`dropReactionTime`** (0.45s) — these
  are what stop it mirroring your movement. They are the difference between "chasing"
  and "hectic".

Shorten any of them to make the fight harder. Reach for the damage number last.

### Communication with the motor

The brain writes, the motor reads, both in `FixedUpdate` — and Unity does not
guarantee which runs first. That is deliberate and safe:

- `MoveX` is a **persistent value**, so a one-step lag is invisible.
- `RequestJump()` is **buffered** (`jumpBufferTime`), so a request never gets dropped
  by ordering, by being made mid-air, or by being made just before landing.

No script execution order is configured, and none is needed.

### Gotchas

- **No physics layers are in use.** Everything is on `Default`. Hitboxes skip only the
  attacker's own hierarchy (`transform.IsChildOf`), which is enough for two fighters —
  but each fighter's *ground check* also sees the other, so standing on the opponent's
  head counts as grounded. Harmless today; friendly fire or projectiles will need real
  layers.
- **`FighterResources` is what identifies the player.** Only the player has one, so
  `FighterHud` finds it and reads the `Health` next to it. Do not add one to the
  opponent without fixing that — `FindAnyObjectByType<Health>()` would otherwise put
  the opponent's HP on the player's bar.
- **Neither fighter respawns.** `Revive()` on either knockout is a debug affordance
  (`R` for the opponent), not a round flow. Both outcomes end the round; `Enter`
  reloads the scene.

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

That search is **empty today** — every setting it flagged has been dealt with, and the
outcomes are in **Split screen** below. The attribute is still there for the next
setting that needs the same treatment.

---

## Known gaps

- **One fight is the whole match.** The loop runs menu → fight → end → menu, but a
  round *is* the match: no score, no best-of-N, no rematch button on the end screen
  (it returns to the menu, and the menu starts a new fight). `RoundOutcome` has
  exactly two values, so a draw has nowhere to go either.
- **No story.** Issue #6 asks for one alongside the screens; the screens exist and
  the story does not.
- **The AI has no defence.** It never blocks, never retreats from a wind-up, and
  cannot be baited. It reacts to *where you are*, never to *what you are doing* — the
  telegraph runs one way only.
- **Climbing plans one hop, not a route.** It works because it re-plans on landing and
  because a hop that completes the climb outscores everything else, but it still cannot
  see two hops ahead. It will not descend in order to climb a better way up, and a
  stage where the only route goes *down* first would leave it `Stranded` or looping.
  Real pathfinding would mean a navigation graph of platform surfaces and jump arcs —
  worth it only if the stage layout gets genuinely maze-like.
- **A platform with no route up is a level-design problem.** `Stranded` is the honest
  failure mode, not a fix: the opponent shadows you from below until you come down.
  Check any new stage has a ladder of surfaces no more than ~3.3 units apart.
- **The quiz cannot lose you the fight.** A wrong answer or a timeout costs nothing
  beyond the Flow that drained meanwhile — see the table under **The quiz half,
  wired**. Whether the puzzle half should be able to actively hurt you is a
  game-design call nobody has made yet.
- **The split ratio is fixed at build time.** `SplitScreenLayout` re-applies on
  resize, but 50/50 is a serialized field, not something the player can drag.
- **No tests.** `FighterResources`, `AttackRunner` and `RoundEvents` are all pure
  logic now and would test well, but test assemblies cannot reference
  `Assembly-CSharp` — testing them requires moving this code into an asmdef first
  (which is exactly why the quiz has one).
- **No text in the HUD.** Bars and pips only. TextMeshPro essentials *are* in the
  project now (they arrived with the menu), so the blocker is gone — nobody has
  added the numbers yet.

## The quiz half, wired (`QuizRewardBridge`)

One component, on the generated quiz root, is the entire integration. It subscribes
to `QuestionResolved` — the event that fires the *instant* an answer lands, rather
than `QuestionAnswered` which waits out the feedback delay — and hands the result to
`RiddleRewards`. Neither half holds a reference to the other; deleting the bridge
leaves both runnable alone.

**The rule it encodes: a correct answer always pays one Action Point, but only a
*fast* one pays Flow.**

| Answer | AP | Flow |
|---|---|---|
| Correct, inside the green zone | +1 | +25 |
| Correct, after the bar turned yellow | +1 | — |
| Wrong | — | — |
| Timed out | — | — |

That asymmetry is the whole point of the pairing. Solving buys you *swings*; solving
**quickly** is the only thing that buys the burst. A player who answers everything
correctly but slowly stays armed and never reaches Flow state.

Wrong answers and timeouts carry no extra penalty, and deliberately so: Flow drains
at 6/s throughout, so a miss has already cost about one solve's worth of progress by
the time the next question appears. Stacking a subtraction on top of that makes a bad
streak unrecoverable rather than merely expensive.

**The threshold is read off `QuizView`, not copied.** `QuizView.FastZoneNormalized`
(0.6) is what decides the timer bar is still green, and the bar's own tooltip already
promised that zone "builds Flow". The bridge asks the view for that number rather
than keeping a second copy, because the failure mode of two copies is a bar the
player watched stay green that then paid nothing — the exact thing the colour exists
to communicate. A `fallbackFastZone` field covers the case where no view is assigned.

Roughly **eight fast solves in a row** reach the 100 Flow needed for Flow state: at
25 a solve against ~12 drained over a 5-second math question's cycle, the net is
about +13. It is meant to be rare and to be earned while also being punched.

Question sequencing is handled by the quiz's own `QuizFlow` — an endless stream that
auto-advances on every resolution including timeouts, which is the auto-reset timer
the design calls for. The generator puts **all four question types** in the mix, at
the same weights and prompts the sample scene uses:

| Type | Weight | Notes |
|---|---|---|
| Authored | 1 | every `QuizQuestion` in `Assets/Quiz/Questions` |
| Generated maths | 1 | always available, 5 s limit |
| Sequences | 1 | numbers and shapes, 8 s limit |
| Wikipedia places | 2 | `Landmarks.asset`, "Which place is this?" |
| Wikipedia animals | 2 | `Animals.asset`, "Which animal is this?" |

The two Wikipedia sources are the only type that can be *unavailable*: each keeps a
small queue filled in the background, counts as available only once one is ready, and
stays empty offline. `QuizFlow` rolls among whatever is available, so a cold queue or
no network costs variety, never a question. See `Assets/Quiz/README.md`.

---

## Round flow

The loop is closed: **menu → fight → end screen → menu**.

```
Scene_Menu ──Start──► PlayerControllerTest ──K.O.──► Scene_End ──Return──► Scene_Menu
                              │                          ▲
                    Health.Died                          │
                              ▼                          │
                    Player/EnemyKnockout                 │
                    (1.2s, so the final                  │
                     knockback plays out)                │
                              ▼                          │
                    RoundEvents.ReportRoundEnded         │
                              ▼                          │
                    RoundFlow ──records──► MatchResult ──┘
                    (waits 1.0s, then loads)   outlives the scene load
```

`RoundFlow` is the real subscriber `RoundEvents` was written for. Combat still reports
through the same static seam and knows nothing about scenes — adding this changed no
combat code at all.

`MatchResult` exists because a scene load destroys everything that knew the outcome.
It is deliberately the smallest thing that can outlive it: one enum and a flag, no
object to find, nothing to wire. It is a *result*, not a save game — nothing is
written to disk.

One scene serves both endings. The difference between winning and losing here is a
line of text and a colour, and two scenes would mean every later layout change being
made twice with the loss screen quietly falling behind. `EndScreen` writes into the
label already in `Scene_End`; opened on its own with no fight behind it, it leaves the
authored text alone rather than claiming a win you did not earn.

### The bug this had been hiding

`RoundEvents.IsRoundOver` is static, and **a scene load does not clear it**. Until
there was a way back to a second fight, that never showed: one fight per Play session
meant the flag was always fresh. With a menu to return from, the second fight of a
session would have started already over — its first `ReportRoundEnded` dropped as a
duplicate, so the round could never end again, and the only symptom would be a fight
that refuses to finish.

`RoundFlow.Awake` reopens the round and clears the last result, before anything can
subscribe or report. That is why the reset lives at the *start* of a fight rather than
at the end of one: an ending that fails to clean up leaves the next fight broken,
whereas a beginning that cleans up first cannot.

### Watch what `DontDestroyOnLoad` drags along

`DebugMenu` in `Scene_Menu` survives scene loads, and it takes **its children** with
it. The `EventSystem` was one of them, so it followed the player into every scene and
sat alongside whichever one that scene already had — uGUI warns about this once per
frame, which is thousands of lines per fight. It is now a scene root instead.

Anything parented under a persistent object becomes persistent too. Each scene here
already has its own `EventSystem`, so nothing needs to travel.

---

## Split screen

The fight takes the **left 75%** of the window, the quiz the remaining 25%.
`SplitScreenLayout` owns the whole arrangement so the ratio exists exactly once, and
re-applies on Start and on any window resize.

| Piece | How it is confined |
|---|---|
| Fighter camera | `camera.rect` — a viewport rect, so *everything* the camera draws is inside it |
| Quiz panel | Re-anchored to the middle of the quiz side, and **scaled** to fit it |
| Quiz backdrop | Full-height image over the quiz side |
| Seam | 4 px divider on the boundary |

**The panel is scaled, not re-flowed**, and that is what makes a narrow quiz side
safe. Its layout is authored at 640 wide; narrowing the `RectTransform` instead keeps
the type at full size and takes the difference out of the answer rows, where an answer
that no longer fits wraps to a second line inside a button whose height is fixed — so
the second line is clipped. Real place and animal names reach that point long before
the panel looks too small. Scaling shrinks type and layout together, so the panel stays
the design that was authored and nothing can wrap that did not wrap before.

At 75/25 the quiz side is 480 units wide of a 1920 reference, less 24 either side, so
the panel renders at **0.675×** — a question label at an effective 27 pt and answers at
20 pt. Give the quiz 36% or more and it renders at full size, since 640 + 48 = 688
units is where the scaling stops. Drag `fighterViewportWidth` and everything else
follows; nothing else needs touching.

**The backdrop is not decoration.** A camera whose viewport covers half the screen
never clears the other half, so without something opaque drawn there the quiz side
shows undefined pixels. It lives on its own canvas at `sortingOrder -100`, with **no
GraphicRaycaster** — a full-height image over the quiz half would otherwise swallow
every answer click.

### What the narrower viewport changed

The `[SplitScreenTodo]` notes flagged six settings. Working through them:

| Setting | Outcome |
|---|---|
| `FollowCamera` bounds | **Derived now.** Was the one marked "MUST be recalculated" |
| `FollowCamera` dead zone width | **Derived now**, scaled by how much world is visible |
| `FollowCamera` look-ahead | Left alone — see below |
| `FollowCamera` offset | Dropped: the quiz has its own viewport and never crowds the fight |
| `FighterHud` anchoring | Already correct — 420 wide at the bottom left of a 960-wide half |
| `PlayerDebugHud` / `EnemyDebugHud` | Already correct — see below |
| `DebugRoundBanner` | **Fixed**: now drawn inside the fighter viewport instead of across the seam |

The bounds were the important one. They are the stage edge minus however much world is
on screen — `stageHalfWidth - tan(fov/2) * distance * aspect` — and narrowing the
viewport narrows the aspect, so the hand-tuned ±5.8 pins the camera to well under the
room it actually has:

| Fighter viewport | Aspect | Sees (half-width) | Bounds |
|---|---|---|---|
| full screen | 1.78 | 9.24 | ±5.76 |
| **75%** | 1.33 | 6.93 | **±8.07** |
| 50% | 0.89 | 4.62 | ±10.38 |

Deriving them means the view stops exactly as the stage edge would come into frame **at
any viewport width**, instead of needing a hand-tuned pair per layout. Set
`deriveHorizontalBounds` false to go back to the literal `boundsMin.x`/`boundsMax.x`.

The dead zone is the same problem in reverse: 3 units is 16% of a full-screen view but
22% of a 75% one and 32% of a half-width one, and a dead zone that large reads as the
camera lagging behind you. It is now scaled by visible width against
`deadZoneReferenceHalfWidth` — 2.25 units at 75% — so the box stays the same *share of
what the player can see*. The vertical half is untouched: split screen changes the
width of the viewport, never its height.

Look-ahead was flagged as wanting a raise and got none: it is specified in world units,
and a narrower viewport already makes the same 2 units lead across a larger share of
the screen. Raising it too would over-lead.

Both debug HUDs turned out to be correct as written, which the notes had guessed wrong.
`EnemyDebugHud` places its label with `WorldToScreenPoint`, which already accounts for
the camera's viewport rect; `PlayerDebugHud` draws at the top left of the window, which
*is* inside the fighter's half. Both would only need work if the fight moved to the
right-hand side.

Nothing carries `[SplitScreenTodo]` any more. The attribute and its drawer are kept
rather than deleted, because the layout is not finished settling — art, level design
and the real results screen are all still to come, and the split ratio is a tunable.
