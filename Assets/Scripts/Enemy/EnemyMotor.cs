using System.Collections.Generic;
using ThinkFast.Anim;
using ThinkFast.Combat;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// The opponent's legs. Same job as
    /// <see cref="ThinkFast.Player.PlayerController"/> -- run, jump, gravity,
    /// ground detection, hitstun -- but driven by whatever writes to
    /// <see cref="MoveX"/> instead of by a keyboard.
    ///
    /// It is a separate class rather than a subclass because the player motor is
    /// [RequireComponent(PlayerInputReader)] and reads input directly. What the
    /// two genuinely share -- the drop-through rules -- lives in
    /// <see cref="OneWayDropThrough"/>, so the awkward part is written once.
    ///
    /// This class makes no decisions. It cannot see the player, does not know
    /// what a target is, and will happily walk into a wall forever. All of that
    /// belongs to <see cref="EnemyBrain"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMotor : MonoBehaviour, IFighterMotor
    {
        [Header("Run")]
        [Tooltip("Top horizontal speed. Kept under the player's 9 on purpose: an opponent that can always close the gap leaves no room to kite it.")]
        [SerializeField] private float maxRunSpeed = 6.5f;

        [Tooltip("How hard we accelerate toward the target speed.")]
        [SerializeField] private float groundAcceleration = 60f;

        [Tooltip("How hard we brake when the brain asks for no movement.")]
        [SerializeField] private float groundDeceleration = 80f;

        [Tooltip("Extra acceleration while reversing direction, so a turnaround is not a long skid.")]
        [SerializeField] private float turnAccelerationMultiplier = 2f;

        [Header("Jump")]
        [Tooltip("Apex height of a jump, in units. Derived into a launch velocity from the current gravity, so tuning gravity does not silently change how high it jumps.\n\nAbove the player's 3.2 on purpose. The apex is also what buys the air time that carries a running jump across a gap, and the platform-to-platform hops in the test stage only clear with a comfortable margin at this height.")]
        [SerializeField] private float jumpHeight = 3.8f;

        [Tooltip("How long after walking off a ledge a jump is still allowed. The brain does not aim jumps precisely, so this mostly stops it fluffing a chase off a platform edge.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Tooltip("A jump requested this long before landing still fires on touchdown. Also absorbs the one-step lag between the brain writing and the motor reading.")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Tooltip("Ground is ignored for this long after launching, so the feet probe cannot immediately hand back a second jump.")]
        [SerializeField] private float jumpGroundLockout = 0.08f;

        [Header("Gravity")]
        [SerializeField] private float gravityScale = 5f;

        [Tooltip("Gravity is multiplied by this while falling. Matched to the player so both fighters fall at a readable, comparable rate.")]
        [SerializeField] private float fallGravityMultiplier = 1.4f;

        [SerializeField] private float maxFallSpeed = 24f;

        [Header("Drop through platforms")]
        [Tooltip("How long collision with a dropped-through platform stays disabled before we even consider restoring it. Must be long enough to fall clear of it.")]
        [SerializeField] private float dropThroughMinDuration = 0.3f;

        [Tooltip("Hard cap, in case we somehow never stop overlapping.")]
        [SerializeField] private float dropThroughMaxDuration = 1.5f;

        [Header("Ground check")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.12f);

        [SerializeField] private float groundCheckDistance = 0.04f;

        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Presentation")]
        [Tooltip("Optional. Yawed 180 degrees so the 3D mesh faces the way we are aiming.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("Optional. Mesh Animator driver on a child object.")]
        [SerializeField] private CharacterAnimation characterAnimation;

        /// <summary>Upward speed above which we are definitely not standing on ground.</summary>
        private const float RisingVelocityEpsilon = 0.1f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private ICharacterAnimation animation;
        private ContactFilter2D groundFilter;
        private OneWayDropThrough dropThrough;

        // Reused so the ground check allocates nothing per physics step.
        private readonly Collider2D[] groundHits = new Collider2D[8];

        // Whatever the feet are currently resting on, so a drop-through knows
        // which platform to disable.
        private readonly List<Collider2D> groundColliders = new List<Collider2D>(4);

        private float coyoteTimer;
        private float jumpBufferTimer;
        private float groundLockoutTimer;
        private float stunTimer;
        private bool dropThroughRequested;

        /// <summary>
        /// Desired horizontal movement, -1 to +1. Persistent: whoever sets it
        /// owns it until they set it again. Read once per physics step.
        /// </summary>
        public float MoveX { get; set; }

        /// <summary>
        /// Scales how much horizontal control the motor has, 0 to 1. Set by
        /// <see cref="EnemyAttack"/> to root the fighter during a swing.
        /// </summary>
        public float MoveControlScale { get; set; } = 1f;

        /// <summary>While true, <see cref="SetFacing"/> is ignored.</summary>
        public bool FacingLocked { get; set; }

        /// <summary>-1 facing left, +1 facing right. Never 0.</summary>
        public float Facing { get; private set; } = 1f;

        public bool IsGrounded { get; private set; }

        public bool IsStunned => stunTimer > 0f;

        /// <summary>
        /// How far a jump rises, in units. Exposed so the brain can work out what
        /// it can actually reach -- without this it can only ever guess, and a
        /// guess that is wrong upward turns into jumping at a ledge forever.
        /// </summary>
        public float JumpApexHeight => jumpHeight;

        /// <summary>
        /// World-space Y of the bottom of the collider. Heights are compared from
        /// the feet, not the centre: what matters for a jump is the surface being
        /// stood on and the surface being aimed at.
        /// </summary>
        public float FeetY => bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y;

        /// <summary>
        /// Downward acceleration currently acting on the fighter, including
        /// whichever fall multiplier is in force. Exposed so the brain can work
        /// out how far it will have dropped by the time a swing connects.
        /// </summary>
        public float FallAcceleration =>
            body != null ? Mathf.Abs(Physics2D.gravity.y) * body.gravityScale : 0f;

        public float MaxRunSpeed => maxRunSpeed;

        /// <summary>
        /// How far it keeps travelling after being told to stop, from full speed.
        /// Anything that asks the fighter to stand on a spot has to tolerate at
        /// least this much overshoot.
        /// </summary>
        public float StoppingDistance =>
            (maxRunSpeed * maxRunSpeed) / (2f * Mathf.Max(0.01f, groundDeceleration));

        /// <summary>
        /// How far it can travel horizontally while at least <paramref name="rise"/>
        /// above the height it jumped from -- in other words, the widest gap it
        /// can cross and still land on something that high.
        ///
        /// Nobody sets this number. It falls out of jump height, gravity, the
        /// fall multiplier and run speed together, which is exactly why it is
        /// worth being able to ask for: change any one of those four and every
        /// platform-to-platform hop in the level silently gets easier or harder.
        ///
        /// Assumes the jump starts at full speed. A hop launched from a standstill
        /// -- which is what a boarding jump is -- covers noticeably less.
        /// </summary>
        public float HorizontalJumpDistance(float rise)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            if (gravity <= 0f || jumpHeight <= 0f)
            {
                return 0f;
            }

            rise = Mathf.Clamp(rise, 0f, jumpHeight);

            float launchSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
            float timeToApex = launchSpeed / gravity;

            // Rising past the height on the way up, and dropping back through it
            // on the way down. The descent uses the fall multiplier, so it is
            // always the shorter half.
            float timeReachingRise = (launchSpeed - Mathf.Sqrt(Mathf.Max(0f, (launchSpeed * launchSpeed) - (2f * gravity * rise)))) / gravity;
            float timeFallingBack = Mathf.Sqrt(2f * (jumpHeight - rise) / (gravity * Mathf.Max(0.01f, fallGravityMultiplier)));

            return maxRunSpeed * Mathf.Max(0f, timeToApex + timeFallingBack - timeReachingRise);
        }

        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        /// <summary>
        /// Asks for a jump. Buffered rather than acted on immediately, so it
        /// survives being requested a step early, mid-air, or a moment before
        /// landing. Component order between the brain and the motor is therefore
        /// irrelevant.
        /// </summary>
        public void RequestJump()
        {
            jumpBufferTimer = jumpBufferTime;
        }

        /// <summary>
        /// Asks to fall through whatever one-way platform we are standing on.
        /// Ignored on solid ground and in mid-air. Cleared every step, so this
        /// must be called continuously to keep meaning it.
        /// </summary>
        public void RequestDropThrough()
        {
            dropThroughRequested = true;
        }

        /// <summary>
        /// Points the fighter left (-1) or right (+1). Facing is aimed rather
        /// than derived from movement: an opponent that backs off while still
        /// looking at you needs to keep its hitbox pointed the right way.
        /// </summary>
        public void SetFacing(float sign)
        {
            if (FacingLocked || sign == 0f)
            {
                return;
            }

            Facing = Mathf.Sign(sign);

            // CharacterAnimation on the mesh owns yaw when present; visualRoot is
            // only rotated for placeholder rigs without an Animator driver.
            if (visualRoot != null && characterAnimation == null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, Facing > 0f ? 0f : 180f, 0f);
            }
        }

        /// <summary>
        /// Takes control away for a moment after being hit. Extends rather than
        /// replaces an existing stun, so a second hit landing during the first
        /// cannot accidentally shorten it.
        /// </summary>
        public void ApplyStun(float duration)
        {
            stunTimer = Mathf.Max(stunTimer, duration);
        }

        /// <summary>Drops the fighter somewhere and kills all momentum and stun. For resets.</summary>
        public void Teleport(Vector2 position)
        {
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            stunTimer = 0f;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            MoveX = 0f;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            if (characterAnimation == null)
            {
                characterAnimation = GetComponentInChildren<CharacterAnimation>();
            }

            animation = characterAnimation;

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
                Debug.LogError($"{nameof(EnemyMotor)} on '{name}' needs a Collider2D.", this);
                enabled = false;
                return;
            }

            dropThrough = new OneWayDropThrough(bodyCollider, dropThroughMinDuration, dropThroughMaxDuration);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            dropThrough.RestoreCleared();
            IsGrounded = CheckGrounded();

            stunTimer -= dt;

            // Hitstun overrides everything below, exactly as it does for the
            // player. Horizontal movement is skipped entirely rather than
            // decelerated -- decelerating would kill the knockback that is meant
            // to be carrying us, which is most of what makes a hit read.
            if (IsStunned)
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                dropThroughRequested = false;
                ApplyGravity();
                return;
            }

            if (dropThroughRequested)
            {
                dropThroughRequested = false;
                if (IsGrounded)
                {
                    dropThrough.Drop(groundColliders);
                }
            }

            groundLockoutTimer -= dt;
            jumpBufferTimer -= dt;

            if (IsGrounded && groundLockoutTimer <= 0f)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= dt;
            }

            ApplyHorizontalMovement();
            ApplyJump();
            ApplyGravity();
        }

        private bool CheckGrounded()
        {
            // Moving upward means we are not standing on anything. Without this,
            // rising through a one-way platform overlaps the feet probe for a
            // frame, refreshes coyote time and hands out a free mid-air jump.
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

                // Our own colliders are never ground. Neither is a platform we
                // are currently falling through, even though the probe still
                // overlaps it -- see OneWayDropThrough.
                if (hit == null || hit.attachedRigidbody == body || dropThrough.IsDroppingThrough(hit))
                {
                    continue;
                }

                groundColliders.Add(hit);
            }

            return groundColliders.Count > 0;
        }

        private void ApplyHorizontalMovement()
        {
            Vector2 velocity = body.linearVelocity;
            float targetSpeed = Mathf.Clamp(MoveX, -1f, 1f) * maxRunSpeed * Mathf.Clamp01(MoveControlScale);

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
            // A buffered request plus ledge forgiveness is what authorises a
            // jump -- being literally grounded this step is neither required nor
            // sufficient. There is no variable height: nothing is holding the
            // button, so every AI jump is a full one.
            if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = CalculateJumpVelocity();
            body.linearVelocity = velocity;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            groundLockoutTimer = jumpGroundLockout;
            animation?.NotifyJump();
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

            body.gravityScale = falling ? gravityScale * fallGravityMultiplier : gravityScale;

            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
                body.linearVelocity = velocity;
            }
        }

        private void OnDisable()
        {
            // IgnoreCollision is a persistent property of the collider pair, so
            // leaving it set would survive this component being switched off --
            // and the enemy IS switched off on knockout.
            dropThrough?.RestoreAll();
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
