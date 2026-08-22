using System;
using System.Collections.Generic;
using ThinkFast.Combat;
using ThinkFast.Economy;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// The fighter's single attack button, resolving to a grounded or airborne
    /// attack depending on where you are when it comes out.
    ///
    /// Runs a startup -> active -> recovery state machine. The hitbox only exists
    /// during the active window, and each swing can hit any given target once.
    ///
    /// Step 3 scope: no AP cost yet. Gating on Action Points, and the Flow
    /// multiplier on knockback, arrive in step 4.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        public enum Phase
        {
            Ready,
            Startup,
            Active,
            Recovery,
        }

        [Header("Attacks")]
        [SerializeField]
        private AttackDefinition groundAttack = new AttackDefinition
        {
            displayName = "Ground Punch",
            startup = 0.15f,
            active = 0.06f,
            recovery = 0.18f,
            damage = 10,
            knockback = new Vector2(11f, 4f),
            hitstun = 0.4f,
            hitboxOffset = new Vector2(0.9f, 0.1f),
            hitboxSize = new Vector2(1.4f, 1.0f),
            moveControlScale = 0f,
            recoveryMoveControlScale = 0.7f,
        };

        [SerializeField]
        private AttackDefinition airAttack = new AttackDefinition
        {
            displayName = "Air Kick",
            startup = 0.08f,
            active = 0.06f,
            recovery = 0.12f,
            damage = 7,
            knockback = new Vector2(7f, 5f),
            hitstun = 0.25f,
            hitboxOffset = new Vector2(0.8f, -0.1f),
            hitboxSize = new Vector2(1.3f, 1.1f),
            moveControlScale = 0.6f,
            recoveryMoveControlScale = 0.9f,
        };

        [Header("Input")]
        [Tooltip("An attack pressed this long before you are able to act still comes out. Set to roughly the length of a recovery, so pressing attack DURING recovery queues the next swing instead of being dropped -- that is what makes consecutive attacks feel responsive.")]
        [SerializeField] private float attackBufferTime = 0.18f;

        [Header("Targets")]
        [Tooltip("Layers the hitbox can hit. Leave as Everything while testing.")]
        [SerializeField] private LayerMask hittableLayers = ~0;

        private PlayerController controller;
        private PlayerInputReader input;

        // Optional. Without it attacks are free, which keeps the fighter testable
        // in isolation from the economy.
        private FighterResources resources;

        private AttackDefinition current;
        private float phaseTimer;
        private float attackBufferTimer;

        // Reused so a swing allocates nothing.
        private readonly Collider2D[] hitResults = new Collider2D[16];

        // One hit per target per swing. Cleared when a new swing starts.
        private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        private ContactFilter2D hitFilter;

        /// <summary>
        /// Raised the instant the hitbox opens, with the attack and the hitbox
        /// centre in world space. Presentation hangs off this rather than being
        /// baked in, so effects can be added or deleted without touching combat.
        /// </summary>
        public event Action<AttackDefinition, Vector2> AttackBecameActive;

        /// <summary>Raised once per target actually hit, with the contact point.</summary>
        public event Action<AttackDefinition, Vector2> HitLanded;

        /// <summary>Raised when attack was pressed but there was no Action Point to pay for it.</summary>
        public event Action AttackRefused;

        /// <summary>Exposed so presentation can tell the two attacks apart by reference.</summary>
        public AttackDefinition GroundAttack => groundAttack;

        /// <summary>Exposed so presentation can tell the two attacks apart by reference.</summary>
        public AttackDefinition AirAttack => airAttack;

        public Phase CurrentPhase { get; private set; } = Phase.Ready;

        /// <summary>Name of the attack in progress, or empty when idle. Debug readout only.</summary>
        public string CurrentAttackName => current != null ? current.displayName : string.Empty;

        public bool IsAttacking => CurrentPhase != Phase.Ready;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            resources = GetComponent<FighterResources>();

            hitFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = hittableLayers,

                // Hitboxes are volumes, not solids: a target's trigger collider
                // is a perfectly good thing to punch.
                useTriggers = true,
            };
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            attackBufferTimer -= dt;
            if (input.ConsumeAttackPress())
            {
                attackBufferTimer = attackBufferTime;
            }

            // Being hit interrupts the swing. Without this you could trade a
            // punch for a punch and still win the exchange, which makes hitstun
            // meaningless.
            if (controller.IsStunned)
            {
                CancelAttack();
                return;
            }

            if (CurrentPhase == Phase.Ready)
            {
                if (attackBufferTimer > 0f)
                {
                    BeginAttack();
                }
            }
            else
            {
                AdvanceAttack(dt);
            }

            // Handed to the controller every step, so ending an attack always
            // restores full control even if a phase was skipped.
            //
            // The lock is per-phase on purpose. Startup and active are the
            // commitment; recovery only has to prevent another ATTACK, not
            // prevent walking. A flat lock across all three leaves you frozen
            // while whatever you just hit sails out of range.
            controller.MoveControlScale = CurrentPhase switch
            {
                Phase.Startup or Phase.Active => current.moveControlScale,
                Phase.Recovery => current.recoveryMoveControlScale,
                _ => 1f,
            };

            // Facing frees up in recovery too: the hitbox is long gone, so
            // turning around cannot be abused.
            controller.FacingLocked = CurrentPhase == Phase.Startup || CurrentPhase == Phase.Active;
        }

        /// <summary>
        /// Drops the swing immediately. The spent Action Point is NOT refunded --
        /// getting hit out of a punch is supposed to cost you.
        /// </summary>
        private void CancelAttack()
        {
            CurrentPhase = Phase.Ready;
            current = null;
            phaseTimer = 0f;
            attackBufferTimer = 0f;
            alreadyHit.Clear();

            controller.MoveControlScale = 1f;
            controller.FacingLocked = false;
        }

        private void BeginAttack()
        {
            // Pay first. The buffered press is spent either way: leaving it
            // queued would retry every physics step for the rest of the buffer
            // window and fire the refusal cue a dozen times over.
            if (resources != null && !resources.TrySpendActionPoint())
            {
                attackBufferTimer = 0f;
                AttackRefused?.Invoke();
                return;
            }

            // Grounded state is sampled once, at the moment the swing starts.
            // Landing mid-punch does not switch you to the other attack.
            current = controller.IsGrounded ? groundAttack : airAttack;

            attackBufferTimer = 0f;
            alreadyHit.Clear();
            CurrentPhase = Phase.Startup;
            phaseTimer = current.startup;
        }

        private void AdvanceAttack(float dt)
        {
            phaseTimer -= dt;

            switch (CurrentPhase)
            {
                case Phase.Startup:
                    if (phaseTimer <= 0f)
                    {
                        CurrentPhase = Phase.Active;
                        phaseTimer = current.active;
                        AttackBecameActive?.Invoke(current, GetHitboxCentre());
                        CheckForHits();
                    }
                    break;

                case Phase.Active:
                    // Sweep every step the hitbox is live, so a target moving
                    // into it mid-window is still caught.
                    CheckForHits();
                    if (phaseTimer <= 0f)
                    {
                        CurrentPhase = Phase.Recovery;
                        phaseTimer = current.recovery;
                    }
                    break;

                case Phase.Recovery:
                    if (phaseTimer <= 0f)
                    {
                        CurrentPhase = Phase.Ready;
                        current = null;
                    }
                    break;
            }
        }

        private void CheckForHits()
        {
            Vector2 centre = GetHitboxCentre();
            int count = Physics2D.OverlapBox(centre, current.hitboxSize, 0f, hitFilter, hitResults);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = hitResults[i];
                if (hit == null)
                {
                    continue;
                }

                // Never punch yourself.
                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                var target = hit.GetComponentInParent<IDamageable>();
                if (target == null || !alreadyHit.Add(target))
                {
                    continue;
                }

                // Flow state scales damage and knockback independently, so the
                // hit can be made to LOOK dramatically bigger without being
                // balanced purely around the damage number.
                float damageMultiplier = resources != null ? resources.DamageMultiplier : 1f;
                float knockbackMultiplier = resources != null ? resources.KnockbackMultiplier : 1f;

                var knockback = new Vector2(
                    current.knockback.x * controller.Facing,
                    current.knockback.y) * knockbackMultiplier;

                int damage = Mathf.Max(1, Mathf.RoundToInt(current.damage * damageMultiplier));

                target.TakeHit(new HitInfo(damage, knockback, current.hitstun, gameObject));
                HitLanded?.Invoke(current, hit.ClosestPoint(centre));
            }
        }

        private Vector2 GetHitboxCentre()
        {
            Vector2 offset = current.hitboxOffset;
            return (Vector2)transform.position + new Vector2(offset.x * controller.Facing, offset.y);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || current == null)
            {
                return;
            }

            // Only the active window draws filled: that is the part that can
            // actually hit something.
            Color colour = CurrentPhase switch
            {
                Phase.Startup => new Color(1f, 0.85f, 0.2f, 0.5f),
                Phase.Active => new Color(1f, 0.2f, 0.2f, 0.9f),
                Phase.Recovery => new Color(0.4f, 0.4f, 0.4f, 0.35f),
                _ => Color.clear,
            };

            if (colour == Color.clear)
            {
                return;
            }

            Gizmos.color = colour;
            var centre = (Vector3)GetHitboxCentre();
            centre.z = transform.position.z;
            var size = new Vector3(current.hitboxSize.x, current.hitboxSize.y, 0.2f);

            if (CurrentPhase == Phase.Active)
            {
                Gizmos.DrawCube(centre, size);
            }
            else
            {
                Gizmos.DrawWireCube(centre, size);
            }
        }
    }
}
