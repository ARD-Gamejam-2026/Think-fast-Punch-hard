using System;
using ThinkFast.Anim;
using ThinkFast.Combat;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// The opponent's single attack, in a ground and an air flavour -- the same
    /// shape as the player's, so both fighters are readable by the same rules.
    ///
    /// The state machine and the hitbox are not implemented here: they are
    /// <see cref="AttackRunner"/>, shared with the player. What is left is the
    /// small part that actually differs. No input, no buffering, no Action
    /// Points: the opponent pays nothing to swing, and is limited by the cooldown
    /// in <see cref="EnemyBrain"/> instead.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor))]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [Header("Attacks")]
        [SerializeField]
        private AttackDefinition groundAttack = new AttackDefinition
        {
            displayName = "Enemy Swipe",

            // Slower startup than the player's 0.15: the wind-up is the window
            // you get to react in, and it is the main dial for how fair the
            // opponent feels. Shorten it to make the fight harder.
            //
            // It cannot go much above this without the opponent looking like it
            // has stalled rather than wound up -- which is why the telegraph in
            // PlaceholderEnemyAttackFx is not optional decoration. A long startup
            // with nothing drawn during it reads as lag.
            startup = 0.2f,
            active = 0.07f,
            recovery = 0.24f,
            damage = 8,
            knockback = new Vector2(9f, 3.5f),
            hitstun = 0.35f,
            hitboxOffset = new Vector2(0.95f, 0.1f),
            hitboxSize = new Vector2(1.5f, 1.0f),
            moveControlScale = 0f,
            recoveryMoveControlScale = 0.5f,
        };

        [SerializeField]
        private AttackDefinition airAttack = new AttackDefinition
        {
            displayName = "Enemy Dive",

            // Much faster than the grounded swing, and it has to be. An air
            // attack happens while arcing past the target at speed: the whole
            // opportunity lasts about a third of a second, and every frame of
            // startup is spent travelling out of range again.
            startup = 0.09f,

            // Longer than the ground attack's window on purpose. It is the only
            // forgiveness the dive gets -- arriving a couple of physics steps off
            // is otherwise the difference between a hit and sailing past.
            active = 0.1f,
            recovery = 0.18f,
            damage = 6,
            knockback = new Vector2(6f, 4.5f),
            hitstun = 0.25f,
            hitboxOffset = new Vector2(0.85f, -0.15f),
            hitboxSize = new Vector2(1.4f, 1.1f),

            // Nearly full control, where the player's air attack keeps 0.6. A
            // dive that brakes itself mid-flight lands short of what it was
            // aimed at, which is self-defeating for an attack whose entire
            // purpose is closing the gap.
            moveControlScale = 0.9f,
            recoveryMoveControlScale = 0.9f,
        };

        [Header("Targets")]
        [Tooltip("Layers the hitbox can hit. Leave as Everything while no physics layers exist.")]
        [SerializeField] private LayerMask hittableLayers = ~0;

        [Header("Presentation")]
        [Tooltip("Optional. Mesh Animator driver on a child object.")]
        [SerializeField] private CharacterAnimation characterAnimation;

        private EnemyMotor motor;
        private AttackRunner runner;
        private ICharacterAnimation animation;

        /// <summary>Raised when a swing is committed to, at the start of startup. The telegraph.</summary>
        public event Action<AttackDefinition, Vector2> AttackStarted;

        /// <summary>Raised the instant the hitbox opens, with the hitbox centre in world space.</summary>
        public event Action<AttackDefinition, Vector2> AttackBecameActive;

        /// <summary>Raised once per target actually hit, with the contact point.</summary>
        public event Action<AttackDefinition, Vector2> HitLanded;

        public AttackRunner.Phase CurrentPhase => runner.CurrentPhase;

        public bool IsAttacking => runner.IsAttacking;

        /// <summary>Name of the attack in progress, or empty when idle. Debug readout only.</summary>
        public string CurrentAttackName => runner.CurrentAttackName;

        /// <summary>Exposed so presentation and tuning can tell the two attacks apart by reference.</summary>
        public AttackDefinition GroundAttack => groundAttack;

        /// <summary>Exposed so presentation and tuning can tell the two attacks apart by reference.</summary>
        public AttackDefinition AirAttack => airAttack;

        /// <summary>
        /// Starts a swing if one is possible. Returns false when already swinging
        /// or stunned, so the caller knows not to start its cooldown.
        /// </summary>
        public bool TryAttack()
        {
            if (runner.IsAttacking || motor.IsStunned)
            {
                return false;
            }

            // Grounded state is sampled once, at the moment the swing starts.
            // Landing mid-swing does not switch to the other attack.
            bool airborne = !motor.IsGrounded;
            runner.Begin(airborne ? airAttack : groundAttack, motor.Facing);
            animation?.NotifyAttackStarted(airborne);
            return true;
        }

        private void Awake()
        {
            motor = GetComponent<EnemyMotor>();

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
            // Being hit interrupts the swing. Without this the opponent could
            // trade a punch for a punch and still win the exchange, which makes
            // hitstun meaningless in both directions.
            if (motor.IsStunned)
            {
                if (runner.IsAttacking)
                {
                    runner.Cancel();
                }

                ReleaseMotor();
                return;
            }

            runner.Tick(Time.fixedDeltaTime, motor.Facing);

            // Handed over every step, so ending a swing always restores full
            // control even if a phase was somehow skipped.
            motor.MoveControlScale = runner.MoveControlScale;
            motor.FacingLocked = runner.LocksFacing;
        }

        private void OnDisable()
        {
            // The component is disabled on knockout, mid-swing more often than
            // not. Leaving the motor rooted and facing-locked would freeze the
            // corpse in place and stop any knockback reading.
            runner?.Cancel();
            ReleaseMotor();
        }

        private void ReleaseMotor()
        {
            if (motor == null)
            {
                return;
            }

            motor.MoveControlScale = 1f;
            motor.FacingLocked = false;
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
