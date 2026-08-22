using System;
using ThinkFast.Anim;
using ThinkFast.Combat;
using ThinkFast.Economy;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// The fighter's single attack button, resolving to a grounded or airborne
    /// attack depending on where you are when it comes out.
    ///
    /// The startup -> active -> recovery machine and the hitbox are not here:
    /// they are <see cref="AttackRunner"/>, shared with the AI opponent so frame
    /// data means the same thing for both fighters. What is left is what makes
    /// this the PLAYER'S attack -- input buffering, paying an Action Point, and
    /// the Flow multipliers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerAttack : MonoBehaviour
    {
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

        [Header("Presentation")]
        [Tooltip("Optional. Mesh Animator driver on a child object.")]
        [SerializeField] private CharacterAnimation characterAnimation;

        private PlayerController controller;
        private PlayerInputReader input;
        private AttackRunner runner;
        private ICharacterAnimation animation;

        // Optional. Without it attacks are free, which keeps the fighter testable
        // in isolation from the economy.
        private FighterResources resources;

        private float attackBufferTimer;

        /// <summary>Raised when a swing is committed to, at the start of startup. The telegraph.</summary>
        public event Action<AttackDefinition, Vector2> AttackStarted;

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

        public AttackRunner.Phase CurrentPhase => runner.CurrentPhase;

        /// <summary>Name of the attack in progress, or empty when idle. Debug readout only.</summary>
        public string CurrentAttackName => runner.CurrentAttackName;

        public bool IsAttacking => runner.IsAttacking;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            resources = GetComponent<FighterResources>();

            if (characterAnimation == null)
            {
                characterAnimation = GetComponentInChildren<CharacterAnimation>();
            }

            animation = characterAnimation;

            runner = new AttackRunner(gameObject, hittableLayers);
            runner.Started += (attack, centre) => AttackStarted?.Invoke(attack, centre);
            runner.BecameActive += (attack, centre) => AttackBecameActive?.Invoke(attack, centre);
            runner.HitLanded += (attack, point) => HitLanded?.Invoke(attack, point);
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

            if (!runner.IsAttacking && attackBufferTimer > 0f)
            {
                BeginAttack();
            }

            // Flow state scales damage and knockback independently, so the hit
            // can be made to LOOK dramatically bigger without being balanced
            // purely around the damage number. Refreshed every step, so a Flow
            // state that ends mid-swing takes effect on the next sweep.
            runner.DamageMultiplier = resources != null ? resources.DamageMultiplier : 1f;
            runner.KnockbackMultiplier = resources != null ? resources.KnockbackMultiplier : 1f;

            runner.Tick(dt, controller.Facing);

            // Handed to the controller every step, so ending an attack always
            // restores full control even if a phase was skipped.
            controller.MoveControlScale = runner.MoveControlScale;
            controller.FacingLocked = runner.LocksFacing;
        }

        /// <summary>
        /// Drops the swing immediately. The spent Action Point is NOT refunded --
        /// getting hit out of a punch is supposed to cost you.
        /// </summary>
        private void CancelAttack()
        {
            runner.Cancel();
            attackBufferTimer = 0f;

            controller.MoveControlScale = 1f;
            controller.FacingLocked = false;
        }

        private void BeginAttack()
        {
            // Pay first -- an Action Point, plus a bite of Flow if Flow state is
            // running. The buffered press is spent either way: leaving it queued
            // would retry every physics step for the rest of the buffer window
            // and fire the refusal cue a dozen times over.
            if (resources != null && !resources.TryPayForAttack())
            {
                attackBufferTimer = 0f;
                AttackRefused?.Invoke();
                return;
            }

            // Grounded state is sampled once, at the moment the swing starts.
            // Landing mid-punch does not switch you to the other attack.
            attackBufferTimer = 0f;
            bool airborne = !controller.IsGrounded;
            runner.Begin(airborne ? airAttack : groundAttack, controller.Facing);
            animation?.NotifyAttackStarted(airborne);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                runner?.DrawGizmos();
            }
        }
    }
}
