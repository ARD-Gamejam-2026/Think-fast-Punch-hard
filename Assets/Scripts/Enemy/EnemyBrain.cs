using ThinkFast.Combat;
using ThinkFast.Player;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// Everything the opponent decides. It reads the world and writes to
    /// <see cref="EnemyMotor"/> and <see cref="EnemyAttack"/>, and touches no
    /// physics of its own -- so how it thinks can be rewritten without any risk
    /// of breaking how it moves.
    ///
    /// The behaviour is deliberately simple and deliberately imperfect: close the
    /// gap, keep a step of spacing, jump at what it cannot reach, swing on a
    /// cooldown. The dials that decide whether it feels fair are the attack
    /// startup (on <see cref="EnemyAttack"/>) and <see cref="reactionTime"/> --
    /// an opponent that swings the instant you enter range is unreadable no
    /// matter how weak the hit is.
    /// </summary>
    /// <remarks>
    /// Runs BEFORE the components it drives. Unity orders same-priority
    /// components by the order they were added, which put EnemyAttack ahead of
    /// this one -- so a swing decided in a given physics step was not ticked
    /// until the step after, and MoveX was always read one step stale. Neither
    /// is catastrophic, but a whole physics step of latency on the swing is
    /// exactly what turns a mid-air punch into a near miss.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyAttack))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        public enum State
        {
            NoTarget,
            Chase,
            Spacing,
            Attack,
            Stunned,

            /// <summary>Heading for a lower platform in order to reach a higher one.</summary>
            Climbing,

            /// <summary>The target is somewhere it has no route to. It stays underneath and waits.</summary>
            Stranded,
        }

        /// <summary>How the opponent currently stands with respect to height.</summary>
        private enum ClimbStatus
        {
            /// <summary>Nothing to climb -- the target is level, below, or one jump away.</summary>
            None,

            /// <summary>Too high for one jump, but a platform in between gets us there.</summary>
            Climbing,

            /// <summary>Too high for one jump and nothing to climb via.</summary>
            Stranded,
        }

        [Header("Target")]
        [Tooltip("Optional. Left empty, the player is found at runtime and re-found if it is ever destroyed.")]
        [SerializeField] private Transform target;

        [Tooltip("How often to look for a target while there is none. Cheap, but no reason to do it every frame.")]
        [SerializeField] private float targetSearchInterval = 0.5f;

        [Header("Spacing")]
        [Tooltip("Horizontal distance it tries to hold. Roughly its own attack range, so it ends up standing where it can actually swing.")]
        [SerializeField] private float preferredDistance = 1.4f;

        [Tooltip("Width of the dead band below the preferred distance in which it neither advances nor retreats. Without one it jitters on the spot.")]
        [SerializeField] private float spacingTolerance = 0.6f;

        [Header("Attacking")]
        [Tooltip("Horizontal reach at which a swing is worth trying. Should sit just inside the hitbox reach, or it whiffs constantly.")]
        [SerializeField] private float attackRange = 1.7f;

        [Tooltip("Vertical difference it will still swing across. Wider than the hitbox, since both fighters keep moving during the startup.")]
        [SerializeField] private float attackVerticalRange = 1.3f;

        [Tooltip("How long the target must be in reach before the first swing comes out. This is the opponent noticing you, and it is what makes the attack readable rather than instant.")]
        [SerializeField] private float reactionTime = 0.25f;

        [Tooltip("The same window while airborne, and much shorter. A jump arcing past the target is only in range for about a third of a second, and the jump was itself the decision -- deliberating a second time mid-flight means never swinging at all, which is what lets a player camp a ledge unpunished.")]
        [SerializeField] private float airReactionTime = 0.12f;

        [Tooltip("Shortest gap between swings.")]
        [SerializeField] private float attackCooldownMin = 1.2f;

        [Tooltip("Longest gap between swings. Randomised across the range so the rhythm cannot be counted out.")]
        [SerializeField] private float attackCooldownMax = 2.2f;

        [Header("Jumping")]
        [Tooltip("How far above it the target must be before it jumps to follow.")]
        [SerializeField] private float jumpToReachHeight = 1.2f;

        [Tooltip("It only jumps after a target that is roughly overhead. Jumping at something high and far away just wastes the arc.")]
        [SerializeField] private float jumpApproachWindow = 6f;

        [Tooltip("How long the target must STAY above it before it jumps. Without this it mirrors every hop the instant you make it, which looks less like an opponent and more like a shadow. Raise it to make the opponent calmer, lower it to make it stick to you.")]
        [SerializeField] private float jumpReactionTime = 0.4f;

        [Tooltip("Minimum gap between REACTION jumps -- following the target upward, or hopping a wall -- so neither turns into a pogo stick. Navigation jumps (boarding a platform, clearing a ledge) deliberately ignore it: they are already limited by having to land first, and blocking one strands the fighter on the platform it just climbed.")]
        [SerializeField] private float jumpCooldown = 1f;

        [Tooltip("How far ahead to look for a wall or step to hop over.")]
        [SerializeField] private float obstacleProbeDistance = 0.7f;

        [Tooltip("What counts as solid ground: walls to hop over, platforms to climb, floor to run out of. Its own colliders and the target are always skipped.")]
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Header("Climbing")]
        [Tooltip("Trimmed off the jump apex when deciding what is reachable. A surface exactly at the apex is not really landable -- you arrive with no margin and the slightest drift misses it.")]
        [SerializeField] private float jumpReachMargin = 0.5f;

        [Tooltip("The region searched for a platform to climb via, centred on the fighter and extending upward from its feet.")]
        [SerializeField] private Vector2 climbScanSize = new Vector2(24f, 10f);

        [Tooltip("Least height a platform must gain to count as progress. Stops it treating a kerb as a route.")]
        [SerializeField] private float minClimbRise = 0.6f;

        [Tooltip("How close to the spot it wants to jump from before it commits.")]
        [SerializeField] private float climbBoardingTolerance = 0.4f;

        [Tooltip("Kept clear of the platform edges when picking where to jump from, so a slight drift still lands on it.")]
        [SerializeField] private float climbEdgeInset = 0.5f;

        [Tooltip("How often the route is re-picked. Not every step: the answer rarely changes and the scan is not free.")]
        [SerializeField] private float climbRescanInterval = 0.4f;

        [Tooltip("How far ahead to look for floor while heading somewhere higher. Finding none means a ledge -- and a running jump off it is usually exactly how a gap to the next platform gets crossed.")]
        [SerializeField] private float ledgeProbeDistance = 0.8f;

        [Header("Dropping down")]
        [Tooltip("How far below it the target must be before it falls through the platform it is standing on.")]
        [SerializeField] private float dropThroughHeight = 1.2f;

        [Tooltip("How long the target must STAY below it before it drops. Same reasoning as the jump reaction: without it, one hop down and back up sends the opponent through the floor after you.")]
        [SerializeField] private float dropReactionTime = 0.45f;

        private EnemyMotor motor;
        private EnemyAttack attack;
        private Rigidbody2D body;
        private Collider2D bodyCollider;

        private ContactFilter2D obstacleFilter;
        private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[8];
        private readonly Collider2D[] climbHits = new Collider2D[24];

        private float nextTargetSearch;
        private float nextAttackTime;
        private float nextJumpTime;
        private float nextClimbScan;

        private ClimbStatus climbStatus;

        // Where to stand in order to jump onto the platform we are routing via.
        // Kept across airborne steps: there is nothing to re-decide mid-jump, and
        // steering toward it is what gives the jump its air control.
        private Vector2 climbBoarding;

        // When the target entered reach / went above / went below, or -1 while it
        // is not. The difference against now is the reaction window, and the
        // reset on the way out is what stops the opponent acting on a condition
        // that only held for a single frame.
        private float inRangeSince = -1f;
        private float aboveSince = -1f;
        private float belowSince = -1f;

        public State CurrentState { get; private set; } = State.NoTarget;

        /// <summary>Whatever it is currently fighting. Null until one is found.</summary>
        public Transform Target => target;

        private void Awake()
        {
            motor = GetComponent<EnemyMotor>();
            attack = GetComponent<EnemyAttack>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            obstacleFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = obstacleLayers,
                useTriggers = false,
            };
        }

        private void OnDisable()
        {
            // Knockout switches this off. Leave the legs at rest rather than
            // whatever the last decision happened to be.
            if (motor != null)
            {
                motor.MoveX = 0f;
            }
        }

        private void FixedUpdate()
        {
            // Hitstun is not a decision to make, it is a decision being taken
            // away. The motor ignores MoveX while stunned anyway; zeroing it here
            // stops the opponent bolting the instant the stun ends.
            if (motor.IsStunned)
            {
                CurrentState = State.Stunned;
                motor.MoveX = 0f;
                ForgetReactionWindows();
                return;
            }

            if (!EnsureTarget())
            {
                CurrentState = State.NoTarget;
                motor.MoveX = 0f;
                ForgetReactionWindows();
                return;
            }

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float horizontalDistance = Mathf.Abs(toTarget.x);
            float directionToTarget = toTarget.x >= 0f ? 1f : -1f;

            // Always look at the target. Ignored mid-swing, when the motor is
            // facing-locked, so the hitbox cannot be steered after it commits.
            motor.SetFacing(directionToTarget);

            TryAttack(toTarget, horizontalDistance);

            UpdateRoute(toTarget);

            // While climbing, the goal is the boarding spot, not the target.
            // Chasing the target's own X would walk straight past the platform
            // that is the only way up to it.
            //
            // This is also the landing commitment. UpdateRoute does not touch
            // climbStatus in mid-air, so a hop keeps steering for the spot it
            // launched at until it lands -- air control is spent getting ONTO
            // the platform, not on drifting toward the target.
            float desiredMove;
            State movementState;
            if (climbStatus == ClimbStatus.Climbing)
            {
                desiredMove = MoveTowardX(climbBoarding.x, climbBoardingTolerance);
                movementState = State.Climbing;
            }
            else
            {
                desiredMove = ChooseMovement(horizontalDistance, directionToTarget, out movementState);
                if (climbStatus == ClimbStatus.Stranded)
                {
                    movementState = State.Stranded;
                }
            }

            motor.MoveX = desiredMove;
            ChooseVerticalMove(toTarget, horizontalDistance, desiredMove);

            // Decided in one place at the end rather than by whichever branch
            // ran last: a state that is only ever assigned inside an if is a
            // state that gets stuck on whatever it was before.
            CurrentState = attack.IsAttacking ? State.Attack : movementState;
        }

        /// <summary>
        /// Works out whether the target's height is even achievable, and if not
        /// directly, what to climb via.
        ///
        /// This is the whole answer to an opponent that pogos under an
        /// unreachable ledge: it was never asking whether the jump could work,
        /// only whether the target was up. Three outcomes --
        ///
        /// - within one jump: nothing to do, chase normally,
        /// - too high but a platform in between helps: go and stand on that instead,
        /// - too high and nothing helps: do not jump at all, shadow it from below.
        ///
        /// The third case is not a great look, but standing under a camper beats
        /// hammering the jump button at it, and it recovers the instant they come
        /// down. A platform genuinely unreachable by any route is a level-design
        /// problem, not one the AI can solve.
        /// </summary>
        private void UpdateRoute(Vector2 toTarget)
        {
            // NOTHING is re-decided in mid-air, and this guard has to come first.
            //
            // Height is measured from where the fighter currently is, so during
            // the hop itself the gap to the target shrinks -- near the apex it
            // drops under UsableRise and the route reads as "no longer needed".
            // Acting on that would abandon the hop at exactly the moment it is
            // half done: the opponent would stop steering for the platform, chase
            // the target instead, sail past the ledge and land back where it
            // started. Forever, because the loop is stable.
            //
            // A jump is a commitment. The route is re-planned on landing, and
            // only on landing.
            if (!motor.IsGrounded)
            {
                return;
            }

            bool needsHeight = toTarget.y > jumpToReachHeight;
            if (!needsHeight || toTarget.y <= UsableRise)
            {
                climbStatus = ClimbStatus.None;
                return;
            }

            if (Time.time >= nextClimbScan)
            {
                nextClimbScan = Time.time + climbRescanInterval;
                climbStatus = TryFindSteppingStone(toTarget, out climbBoarding)
                    ? ClimbStatus.Climbing
                    : ClimbStatus.Stranded;
            }
        }

        /// <summary>
        /// Looks for a surface that is above us, reachable in one jump, and not
        /// already higher than the target. Returns where on it to jump from.
        ///
        /// Not a pathfinder -- it plans exactly one hop. That turns out to be
        /// enough, because it re-plans on every landing: each hop makes the next
        /// one reachable, and a two-platform climb emerges without anything
        /// having to reason about the whole route.
        /// </summary>
        private bool TryFindSteppingStone(Vector2 toTarget, out Vector2 boarding)
        {
            boarding = default;

            if (bodyCollider == null)
            {
                return false;
            }

            float standY = motor.FeetY;
            float targetX = target.position.x;

            // Both fighters use the same collider, so the centre-to-centre
            // difference is also the feet-to-feet difference. Heights are
            // compared from the feet throughout: what matters is the surface
            // being stood on, not where the middle of the body happens to be.
            float targetFeetY = standY + toTarget.y;

            var scanCentre = new Vector2(transform.position.x, standY + (climbScanSize.y * 0.5f));
            int count = Physics2D.OverlapBox(scanCentre, climbScanSize, 0f, obstacleFilter, climbHits);

            float bestScore = float.MinValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = climbHits[i];
                if (candidate == null || candidate.attachedRigidbody == body)
                {
                    continue;
                }

                if (target != null && candidate.transform.IsChildOf(target))
                {
                    continue;
                }

                Bounds bounds = candidate.bounds;
                float surfaceY = bounds.max.y;
                float rise = surfaceY - standY;

                // Has to actually gain height, and has to be gettable in one go.
                // The upper bound is the entire point of this method.
                if (rise < minClimbRise || rise > UsableRise)
                {
                    continue;
                }

                // Never route via something already above the target -- that is
                // overshooting, and it would climb away from what it is chasing.
                if (surfaceY > targetFeetY + minClimbRise)
                {
                    continue;
                }

                float inset = Mathf.Min(climbEdgeInset, bounds.extents.x * 0.5f);
                float boardX = Mathf.Clamp(targetX, bounds.min.x + inset, bounds.max.x - inset);

                // A stone that leaves the target within one more jump wins
                // outright. This is the closest thing here to actually planning:
                // it cannot see a whole route, but it can see whether THIS hop
                // finishes the job, which is what keeps it off dead ends
                // whenever a completing stone exists at all.
                bool completesClimb = targetFeetY - surfaceY <= UsableRise;

                // Failing that: highest first, because height is what we are
                // short of. Ties broken by how much closer it leaves us to
                // standing under the target, which picks the right side of the
                // stage.
                float score = (completesClimb ? 1000f : 0f)
                    + (rise * 10f)
                    - Mathf.Abs(boardX - targetX);
                if (score > bestScore)
                {
                    bestScore = score;
                    boarding = new Vector2(boardX, surfaceY);
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// How much height a jump can actually convert into a landing. Derived
        /// from the motor rather than set, so re-tuning the jump re-tunes what the
        /// opponent believes it can reach.
        /// </summary>
        public float UsableRise => Mathf.Max(0f, motor.JumpApexHeight - jumpReachMargin);

        /// <summary>Exposed for <see cref="EnemyTuningCheck"/>, which cross-checks it against the hitbox.</summary>
        public float AttackRange => attackRange;

        /// <summary>Exposed for <see cref="EnemyTuningCheck"/>, which cross-checks it against the attack range.</summary>
        public float PreferredDistance => preferredDistance;

        /// <summary>Exposed for <see cref="EnemyTuningCheck"/>, which cross-checks it against the stopping distance.</summary>
        public float ClimbEdgeInset => climbEdgeInset;

        private float MoveTowardX(float goalX, float tolerance)
        {
            float delta = goalX - transform.position.x;
            return Mathf.Abs(delta) <= tolerance ? 0f : Mathf.Sign(delta);
        }

        /// <summary>
        /// Finds the player, and keeps looking if there is not one yet. The
        /// scene builder wires the target directly; this is the fallback for a
        /// hand-placed opponent, or one that outlives the fighter it was aimed
        /// at. Rate-limited because a scene-wide search per frame is not free.
        /// </summary>
        private bool EnsureTarget()
        {
            if (target != null)
            {
                return true;
            }

            if (Time.time < nextTargetSearch)
            {
                return false;
            }

            nextTargetSearch = Time.time + targetSearchInterval;

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }

            return target != null;
        }

        private void TryAttack(Vector2 toTarget, float horizontalDistance)
        {
            if (attack.IsAttacking)
            {
                return;
            }

            bool airborne = !motor.IsGrounded;
            AttackDefinition definition = airborne ? attack.AirAttack : attack.GroundAttack;

            // "Is the target in reach?" is the wrong question. The hitbox does
            // not exist until startup has elapsed, so the right question is
            // whether they are in reach of where the fighter WILL BE when it
            // opens. Aim where you are going, not where you are.
            //
            // Both axes matter, and the vertical one is the easy one to forget.
            // Horizontally, a jump closes most of an attack range during startup,
            // so without the lead the opponent arcs past a ledge-camper and only
            // notices them once the swing could no longer have landed.
            // Vertically, a fighter blocked by the target's own body loses its
            // horizontal speed and simply FALLS -- so the hitbox opens below what
            // it was aimed at. That is the near miss that makes standing on the
            // edge of a platform safe.
            Vector2 predicted = toTarget - PredictedTravel(definition, airborne);

            bool inReach = Mathf.Abs(predicted.x) <= attackRange
                && Mathf.Abs(predicted.y) <= attackVerticalRange;

            if (!inReach)
            {
                inRangeSince = -1f;
                return;
            }

            if (inRangeSince < 0f)
            {
                inRangeSince = Time.time;
            }

            bool reacted = Time.time - inRangeSince >= (airborne ? airReactionTime : reactionTime);
            if (!reacted || Time.time < nextAttackTime)
            {
                return;
            }

            if (attack.TryAttack())
            {
                // Randomised so the opponent cannot be metronomed. Rolled after
                // the swing lands rather than before, so the gap is measured from
                // the swing that actually happened.
                nextAttackTime = Time.time + Random.Range(attackCooldownMin, attackCooldownMax);
            }
        }

        /// <summary>
        /// How far the fighter itself will move between committing to a swing
        /// and the hitbox opening.
        ///
        /// Horizontal travel is scaled by the attack's own
        /// <see cref="AttackDefinition.moveControlScale"/>, because the swing
        /// brakes the fighter -- predicting off the current speed would
        /// consistently overshoot and make it swing too early.
        ///
        /// Vertical travel only matters in the air, where the drop during
        /// startup is easily half a hitbox.
        /// </summary>
        private Vector2 PredictedTravel(AttackDefinition definition, bool airborne)
        {
            float startup = definition.startup;
            Vector2 velocity = motor.Velocity;

            var travel = new Vector2(velocity.x * definition.moveControlScale * startup, 0f);

            if (airborne)
            {
                travel.y = (velocity.y * startup)
                    - (0.5f * motor.FallAcceleration * startup * startup);
            }

            return travel;
        }

        /// <summary>
        /// Advance, hold, or give ground. The dead band between advancing and
        /// retreating is what stops it vibrating against the player capsule.
        /// </summary>
        private float ChooseMovement(float horizontalDistance, float directionToTarget, out State state)
        {
            if (horizontalDistance > preferredDistance)
            {
                state = State.Chase;
                return directionToTarget;
            }

            state = State.Spacing;

            return horizontalDistance < preferredDistance - spacingTolerance
                ? -directionToTarget
                : 0f;
        }

        /// <summary>
        /// Jumping and dropping, both behind a reaction window.
        ///
        /// The windows are the whole point. Chasing vertically on the frame the
        /// condition becomes true means the opponent copies every hop you make,
        /// instantly -- which does not read as an opponent reacting, it reads as
        /// a mirror. Requiring the condition to HOLD for a moment means a quick
        /// hop is ignored and only actually going somewhere is followed.
        ///
        /// Being blocked by geometry is exempt: that is not a reaction to the
        /// player, it is being stuck, and hesitating about it just looks broken.
        /// </summary>
        private void ChooseVerticalMove(Vector2 toTarget, float horizontalDistance, float desiredMove)
        {
            bool wantsToDrop = motor.IsGrounded && toTarget.y < -dropThroughHeight;
            belowSince = Track(wantsToDrop, belowSince);

            // Only counted while the target is actually within one jump. A
            // window that fills up under an unreachable ledge is what produced
            // the endless hopping.
            bool wantsToJump = motor.IsGrounded
                && climbStatus == ClimbStatus.None
                && toTarget.y > jumpToReachHeight
                && horizontalDistance < jumpApproachWindow;
            aboveSince = Track(wantsToJump, aboveSince);

            if (!motor.IsGrounded)
            {
                return;
            }

            // Chase downward by falling through whatever we are standing on.
            // Harmlessly ignored on solid ground, so no check is needed for
            // whether this even is a platform.
            if (HasHeld(belowSince, dropReactionTime))
            {
                motor.RequestDropThrough();
                return;
            }

            // Wanting to be higher is what separates "the floor ran out" from
            // "walk off it, then". Stranded deliberately never wants height --
            // there is nowhere up there it can get to.
            bool wantsHeight = climbStatus == ClimbStatus.Climbing
                || (climbStatus == ClimbStatus.None && toTarget.y > jumpToReachHeight);

            bool moving = desiredMove != 0f;

            // Standing where we meant to stand, under the platform we are
            // routing via. No reaction window: this is navigation, not a
            // reaction to the player.
            bool atBoardingSpot = climbStatus == ClimbStatus.Climbing
                && Mathf.Abs(climbBoarding.x - transform.position.x) <= climbBoardingTolerance;

            // About to run out of floor while heading somewhere higher. A running
            // jump off the edge is how a gap between two platforms gets crossed,
            // and hesitating here just means falling down a level instead.
            bool atLedge = moving && wantsHeight && IsAtLedge(desiredMove);

            // NAVIGATION JUMPS IGNORE THE COOLDOWN, and they have to.
            //
            // A hop onto a platform is airborne for well under the cooldown, so
            // the fighter lands still holding it -- and the ledge it needs to
            // jump from next is a fraction of a second's walk away. It would
            // detect the ledge, be refused, walk off, fall, climb again, and
            // repeat forever. The loop looks like indecision; it is actually the
            // cooldown outliving the platform.
            //
            // Exempting them cannot produce rapid fire: both require being
            // grounded, and a jump costs a good half-second in the air. The
            // cooldown is left where it belongs -- on jumps that are a REACTION,
            // which is where repeated firing would read as twitchiness.
            if (atBoardingSpot || atLedge)
            {
                Jump();
                return;
            }

            if (Time.time < nextJumpTime)
            {
                return;
            }

            bool blocked = moving && IsBlockedAhead(desiredMove);

            if (blocked || HasHeld(aboveSince, jumpReactionTime))
            {
                Jump();
            }
        }

        private void Jump()
        {
            motor.RequestJump();
            nextJumpTime = Time.time + jumpCooldown;

            // Spent. Landing next to a target that is still above restarts the
            // window rather than launching again the moment the cooldown lapses.
            aboveSince = -1f;
        }

        /// <summary>
        /// Is the floor about to run out ahead? Probed downward from just past
        /// the feet, so it fires while there is still time to jump rather than
        /// once already falling.
        /// </summary>
        private bool IsAtLedge(float direction)
        {
            if (bodyCollider == null)
            {
                return false;
            }

            Bounds bounds = bodyCollider.bounds;
            var origin = new Vector2(
                bounds.center.x + (Mathf.Sign(direction) * ledgeProbeDistance),
                bounds.min.y + 0.05f);

            int count = Physics2D.Raycast(origin, Vector2.down, obstacleFilter, obstacleHits, ledgeProbeDistance);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = obstacleHits[i].collider;
                if (hit == null || hit.attachedRigidbody == body)
                {
                    continue;
                }

                if (target != null && hit.transform.IsChildOf(target))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Drops every part-built reaction. Called when the opponent stops being
        /// able to act at all -- coming out of hitstun with a window already
        /// three-quarters full would produce exactly the instant reaction the
        /// windows exist to prevent.
        /// </summary>
        private void ForgetReactionWindows()
        {
            inRangeSince = -1f;
            aboveSince = -1f;
            belowSince = -1f;

            // The route is stale too -- being knocked across the stage tends to
            // invalidate whatever platform we had picked. Forcing a rescan is
            // cheaper than acting on the old answer for up to a rescan interval.
            nextClimbScan = 0f;
        }

        /// <summary>Starts a window when a condition becomes true, and clears it the moment it stops.</summary>
        private static float Track(bool condition, float since)
        {
            if (!condition)
            {
                return -1f;
            }

            return since < 0f ? Time.time : since;
        }

        private static bool HasHeld(float since, float duration)
        {
            return since >= 0f && Time.time - since >= duration;
        }

        /// <summary>
        /// Is something solid in the way at shin height? Probing low catches both
        /// walls and the kind of low step that a chase would otherwise grind
        /// against forever.
        /// </summary>
        private bool IsBlockedAhead(float direction)
        {
            if (bodyCollider == null)
            {
                return false;
            }

            Bounds bounds = bodyCollider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y + 0.25f);
            var castDirection = new Vector2(Mathf.Sign(direction), 0f);

            int count = Physics2D.Raycast(origin, castDirection, obstacleFilter, obstacleHits, obstacleProbeDistance);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = obstacleHits[i].collider;
                if (hit == null || hit.attachedRigidbody == body)
                {
                    continue;
                }

                // The player is something to punch, not something to hop over.
                if (target != null && hit.transform.IsChildOf(target))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
