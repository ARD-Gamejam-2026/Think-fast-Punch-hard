using System.Collections.Generic;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// Fighter movement. 3D art, 2D physics: the visual child is a mesh, but
    /// everything that moves or collides here is <see cref="Rigidbody2D"/>.
    ///
    /// Step 2 scope: horizontal movement, ground detection, facing, and jumping
    /// with coyote time, input buffering and variable height. Dedicated air
    /// control, double jump and attacking arrive in later steps -- until then the
    /// airborne fighter simply keeps full ground acceleration.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Run")]
        [Tooltip("Top horizontal speed, in units per second.")]
        [SerializeField] private float maxRunSpeed = 9f;

        [Tooltip("How hard we accelerate toward the target speed.")]
        [SerializeField] private float groundAcceleration = 90f;

        [Tooltip("How hard we brake when the keys are released.")]
        [SerializeField] private float groundDeceleration = 110f;

        [Tooltip("Extra acceleration while reversing direction. This is what makes a turnaround feel snappy rather than skiddy.")]
        [SerializeField] private float turnAccelerationMultiplier = 2f;

        [Tooltip("Input below this magnitude counts as no input at all.")]
        [SerializeField, Range(0f, 0.9f)] private float inputDeadzone = 0.2f;

        [Header("Jump")]
        [Tooltip("Apex height of a full-hold jump, in units. The launch velocity is derived from this and the current gravity, so tuning gravity does not silently change how high you jump.")]
        [SerializeField] private float jumpHeight = 3.2f;

        [Tooltip("Fraction of upward velocity kept when the jump key is released early. Lower means tapping gives a much shorter hop.")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.45f;

        [Tooltip("How long after walking off a ledge you can still jump. Without this, players are convinced the game ignored their input.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Tooltip("A jump pressed this long before landing still fires on touchdown, instead of being dropped.")]
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Tooltip("Ground is ignored for this long after launching, so the feet probe cannot immediately hand back a second jump.")]
        [SerializeField] private float jumpGroundLockout = 0.08f;

        [Header("Gravity")]
        [Tooltip("Base gravity while grounded or rising. Fighters want heavier-than-real gravity.")]
        [SerializeField] private float gravityScale = 5f;

        [Tooltip("Gravity is multiplied by this while falling. Falling faster than you rose is most of what stops a jump feeling floaty.")]
        [SerializeField] private float fallGravityMultiplier = 1.4f;

        [Tooltip("Terminal velocity, so long drops stay readable.")]
        [SerializeField] private float maxFallSpeed = 24f;

        [Header("Fast fall")]
        [Tooltip("Gravity multiplier while holding down in the air. Replaces the normal fall multiplier rather than stacking with it.")]
        [SerializeField] private float fastFallGravityMultiplier = 3.2f;

        [Tooltip("Terminal velocity while fast falling.")]
        [SerializeField] private float fastFallMaxSpeed = 34f;

        [Header("Drop through platforms")]
        [Tooltip("How long collision with a dropped-through platform stays disabled before we even consider restoring it. Must be long enough to fall clear of it.")]
        [SerializeField] private float dropThroughMinDuration = 0.3f;

        [Tooltip("Hard cap, in case we somehow never stop overlapping. Prevents a platform being ignored forever.")]
        [SerializeField] private float dropThroughMaxDuration = 1.5f;

        [Header("Ground check")]
        [Tooltip("Size of the box probed just under the collider's feet.")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.12f);

        [Tooltip("How far below the collider bottom the probe box sits.")]
        [SerializeField] private float groundCheckDistance = 0.04f;

        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Presentation")]
        [Tooltip("Optional. Yawed 180 degrees so the 3D mesh faces the way we are moving.")]
        [SerializeField] private Transform visualRoot;

        /// <summary>Upward speed above which we are definitely not standing on ground.</summary>
        private const float RisingVelocityEpsilon = 0.1f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private PlayerInputReader input;
        private ContactFilter2D groundFilter;

        // Reused so the ground check allocates nothing per physics step.
        private readonly Collider2D[] groundHits = new Collider2D[8];

        // Whatever the feet are currently resting on. Needed so a drop-through
        // knows which platform to disable.
        private readonly List<Collider2D> groundColliders = new List<Collider2D>(4);

        /// <summary>
        /// A platform we are currently falling through. Collision is disabled per
        /// collider pair, so only this fighter passes through -- rotating the
        /// effector instead would let everything else through as well.
        /// </summary>
        private struct DroppedPlatform
        {
            public Collider2D Collider;
            public float EarliestRestore;
            public float ForcedRestore;
        }

        private readonly List<DroppedPlatform> droppedPlatforms = new List<DroppedPlatform>(4);

        private float coyoteTimer;
        private float jumpBufferTimer;
        private float groundLockoutTimer;
        private float stunTimer;

        // Set on launch, cleared once the early-release cut has been applied or
        // we land. Without it, holding jump through the apex would re-cut on the
        // way down.
        private bool jumpCutPending;

        /// <summary>True while the feet probe is touching something solid.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>True while a ledge-forgiveness jump is still available.</summary>
        public bool HasCoyote => coyoteTimer > 0f;

        /// <summary>True while a recent jump press is still waiting to be spent.</summary>
        public bool HasBufferedJump => jumpBufferTimer > 0f;

        /// <summary>True while down is held during a descent.</summary>
        public bool IsFastFalling { get; private set; }

        /// <summary>True while falling through a one-way platform.</summary>
        public bool IsDroppingThroughPlatform => droppedPlatforms.Count > 0;

        /// <summary>True while hitstun has taken control away.</summary>
        public bool IsStunned => stunTimer > 0f;

        /// <summary>
        /// Takes control away for a moment after being hit. Extends rather than
        /// replaces an existing stun, so a second hit landing during the first
        /// cannot accidentally shorten it.
        /// </summary>
        public void ApplyStun(float duration)
        {
            stunTimer = Mathf.Max(stunTimer, duration);
        }

        /// <summary>-1 facing left, +1 facing right. Never 0.</summary>
        public float Facing { get; private set; } = 1f;

        /// <summary>
        /// Scales how much horizontal control the player has, 0 to 1. Set by
        /// <see cref="PlayerAttack"/> to root the fighter during a swing. Reset to
        /// 1 every step by whoever lowered it, so a dropped state cannot leave the
        /// player permanently stuck.
        /// </summary>
        public float MoveControlScale { get; set; } = 1f;

        /// <summary>
        /// While true, facing stops following input. Set during an attack so the
        /// hitbox cannot be flipped to the other side mid-swing.
        /// </summary>
        public bool FacingLocked { get; set; }

        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            input = GetComponent<PlayerInputReader>();

            // Configure the body from code so a scene that was set up by hand
            // cannot silently drift away from what the movement maths assumes.
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = gravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayers,
                useTriggers = false,
            };

            if (bodyCollider == null)
            {
                Debug.LogError($"{nameof(PlayerController)} on '{name}' needs a Collider2D.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            RestoreClearedPlatforms();
            IsGrounded = CheckGrounded();

            stunTimer -= dt;

            // Hitstun overrides everything below. Input is drained rather than
            // ignored so nothing queues up and fires the instant stun ends, and
            // horizontal movement is skipped entirely -- decelerating here would
            // kill the knockback that is meant to be carrying us.
            if (IsStunned)
            {
                input.ConsumeJumpPress();
                input.ConsumeDownPress();
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                ApplyGravity();
                return;
            }

            if (input.ConsumeDownPress() && IsGrounded)
            {
                TryDropThroughPlatform();
            }

            groundLockoutTimer -= dt;
            jumpBufferTimer -= dt;

            // Refresh ledge forgiveness only once we are genuinely off the
            // ground and past the launch lockout.
            if (IsGrounded && groundLockoutTimer <= 0f)
            {
                coyoteTimer = coyoteTime;
                jumpCutPending = false;
            }
            else
            {
                coyoteTimer -= dt;
            }

            if (input.ConsumeJumpPress())
            {
                jumpBufferTimer = jumpBufferTime;
            }

            ApplyHorizontalMovement();
            ApplyJump();
            ApplyGravity();
        }

        private void Update()
        {
            UpdateFacing();
        }

        private bool CheckGrounded()
        {
            // Moving upward means we are not standing on anything. This matters
            // for one-way platforms: while rising through one, the feet probe
            // would otherwise overlap it for a frame, refresh coyote time and
            // hand out a free second jump in mid-air.
            groundColliders.Clear();

            if (body.linearVelocity.y > RisingVelocityEpsilon)
            {
                return false;
            }

            Bounds bounds = bodyCollider.bounds;
            var probeCentre = new Vector2(
                bounds.center.x,
                bounds.min.y - groundCheckDistance - (groundCheckSize.y * 0.5f));

            int count = Physics2D.OverlapBox(probeCentre, groundCheckSize, 0f, groundFilter, groundHits);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = groundHits[i];

                // Our own colliders are never ground.
                if (hit == null || hit.attachedRigidbody == body)
                {
                    continue;
                }

                // A platform we are currently dropping through is not ground,
                // even though the probe still overlaps it. OverlapBox is a query
                // and does not respect IgnoreCollision, so this has to be
                // filtered by hand -- otherwise falling through a platform would
                // keep refreshing coyote time on the way down.
                if (IsDroppingThrough(hit))
                {
                    continue;
                }

                groundColliders.Add(hit);
            }

            return groundColliders.Count > 0;
        }

        private bool IsDroppingThrough(Collider2D candidate)
        {
            for (int i = 0; i < droppedPlatforms.Count; i++)
            {
                if (droppedPlatforms[i].Collider == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Disables collision with whatever one-way platform we are standing on,
        /// so we fall through it. Solid ground is left alone: pressing down on
        /// the floor should do nothing.
        /// </summary>
        private void TryDropThroughPlatform()
        {
            for (int i = 0; i < groundColliders.Count; i++)
            {
                Collider2D ground = groundColliders[i];
                if (ground == null || ground.GetComponent<PlatformEffector2D>() == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(bodyCollider, ground, true);
                droppedPlatforms.Add(new DroppedPlatform
                {
                    Collider = ground,
                    EarliestRestore = Time.time + dropThroughMinDuration,
                    ForcedRestore = Time.time + dropThroughMaxDuration,
                });
            }
        }

        /// <summary>
        /// Re-enables collision once we are genuinely clear of a platform.
        /// Restoring while still overlapping would have the solver shove us out,
        /// which reads as being spat back onto the platform we just left.
        /// </summary>
        private void RestoreClearedPlatforms()
        {
            for (int i = droppedPlatforms.Count - 1; i >= 0; i--)
            {
                DroppedPlatform dropped = droppedPlatforms[i];

                if (dropped.Collider == null)
                {
                    droppedPlatforms.RemoveAt(i);
                    continue;
                }

                bool waitedLongEnough = Time.time >= dropped.EarliestRestore;
                bool outOfPatience = Time.time >= dropped.ForcedRestore;
                bool stillOverlapping = Physics2D.Distance(bodyCollider, dropped.Collider).isOverlapped;

                if (outOfPatience || (waitedLongEnough && !stillOverlapping))
                {
                    Physics2D.IgnoreCollision(bodyCollider, dropped.Collider, false);
                    droppedPlatforms.RemoveAt(i);
                }
            }
        }

        private void OnDisable()
        {
            // IgnoreCollision is a persistent property of the collider pair, so
            // leaving it set would survive this component being switched off.
            for (int i = 0; i < droppedPlatforms.Count; i++)
            {
                if (droppedPlatforms[i].Collider != null && bodyCollider != null)
                {
                    Physics2D.IgnoreCollision(bodyCollider, droppedPlatforms[i].Collider, false);
                }
            }

            droppedPlatforms.Clear();
        }

        private void ApplyHorizontalMovement()
        {
            float moveX = input.MoveX;
            if (Mathf.Abs(moveX) < inputDeadzone)
            {
                moveX = 0f;
            }

            Vector2 velocity = body.linearVelocity;
            float targetSpeed = moveX * maxRunSpeed * Mathf.Clamp01(MoveControlScale);

            float acceleration;
            if (Mathf.Approximately(targetSpeed, 0f))
            {
                acceleration = groundDeceleration;
            }
            else if (velocity.x != 0f && Mathf.Sign(targetSpeed) != Mathf.Sign(velocity.x))
            {
                acceleration = groundAcceleration * turnAccelerationMultiplier;
            }
            else
            {
                acceleration = groundAcceleration;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        private void ApplyJump()
        {
            Vector2 velocity = body.linearVelocity;

            // A buffered press plus ledge forgiveness is what actually authorises
            // a jump -- being literally grounded this frame is neither required
            // nor sufficient.
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                velocity.y = CalculateJumpVelocity();
                body.linearVelocity = velocity;

                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                groundLockoutTimer = jumpGroundLockout;
                jumpCutPending = true;
                return;
            }

            // Variable height: releasing early trims the rise. Only ever applied
            // once per jump, and only while still going up.
            if (jumpCutPending && !input.JumpHeld && velocity.y > 0f)
            {
                velocity.y *= jumpCutMultiplier;
                body.linearVelocity = velocity;
                jumpCutPending = false;
            }
        }

        /// <summary>
        /// Derives launch speed from the desired apex height, so re-tuning
        /// gravity does not silently change how high the fighter jumps.
        /// </summary>
        private float CalculateJumpVelocity()
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            return Mathf.Sqrt(2f * gravity * Mathf.Max(0f, jumpHeight));
        }

        private void ApplyGravity()
        {
            Vector2 velocity = body.linearVelocity;
            bool falling = velocity.y < 0f;

            // Fast fall only applies on the way down. Holding down through the
            // rise would otherwise cut the jump short, which reads as the jump
            // being broken rather than as a technique.
            IsFastFalling = falling && !IsGrounded && input.DownHeld;

            if (falling)
            {
                body.gravityScale = gravityScale *
                    (IsFastFalling ? fastFallGravityMultiplier : fallGravityMultiplier);
            }
            else
            {
                body.gravityScale = gravityScale;
            }

            float terminal = IsFastFalling ? fastFallMaxSpeed : maxFallSpeed;
            if (velocity.y < -terminal)
            {
                velocity.y = -terminal;
                body.linearVelocity = velocity;
            }
        }

        private void UpdateFacing()
        {
            if (FacingLocked)
            {
                return;
            }

            float moveX = input.MoveX;
            if (Mathf.Abs(moveX) < inputDeadzone)
            {
                return;
            }

            Facing = Mathf.Sign(moveX);
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, Facing > 0f ? 0f : 180f, 0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D probe = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (probe == null)
            {
                return;
            }

            Bounds bounds = probe.bounds;
            var probeCentre = new Vector3(
                bounds.center.x,
                bounds.min.y - groundCheckDistance - (groundCheckSize.y * 0.5f),
                transform.position.z);

            Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(probeCentre, new Vector3(groundCheckSize.x, groundCheckSize.y, 0.1f));
        }
    }
}
