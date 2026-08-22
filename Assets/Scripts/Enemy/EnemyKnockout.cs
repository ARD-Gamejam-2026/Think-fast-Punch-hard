using System;
using ThinkFast.Combat;
using ThinkFast.Rounds;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// What a knocked-out opponent means for the fight.
    ///
    /// Split from <see cref="FighterHitReaction"/> on the same principle that
    /// splits <see cref="Health"/> from a hit reaction: one component turns a hit
    /// into a body reacting, this one turns the last hit into a round ending.
    /// Neither knows what the other does.
    ///
    /// It deliberately does not load a scene or show anything. It reports through
    /// <see cref="RoundEvents"/> and stops there, so when a real match flow
    /// arrives it subscribes and this file does not change.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyMotor))]
    public sealed class EnemyKnockout : MonoBehaviour, IFighterKnockout
    {
        [Tooltip("Seconds between the killing blow and the round being reported over. Long enough for the final knockback to play out -- ending the round on the exact frame of the hit throws away the best-looking moment in the fight.")]
        [SerializeField] private float roundEndDelay = 1.2f;

        private Health health;
        private EnemyMotor motor;
        private EnemyBrain brain;
        private EnemyAttack attack;
        private Rigidbody2D body;

        private Vector2 spawnPosition;
        private float roundEndTimer;

        /// <summary>Raised the moment health hits zero, before the round-end delay.</summary>
        public event Action KnockedOut;

        /// <summary>Raised when the opponent is put back on its feet. Debug affordance only.</summary>
        public event Action Revived;

        public bool IsKnockedOut { get; private set; }

        private void Awake()
        {
            health = GetComponent<Health>();
            motor = GetComponent<EnemyMotor>();
            brain = GetComponent<EnemyBrain>();
            attack = GetComponent<EnemyAttack>();
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
        }

        private void OnEnable()
        {
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (IsKnockedOut)
            {
                return;
            }

            IsKnockedOut = true;
            SetFightingComponents(false);
            KnockedOut?.Invoke();

            roundEndTimer = roundEndDelay;
        }

        private void Update()
        {
            if (!IsKnockedOut || roundEndTimer <= 0f)
            {
                return;
            }

            roundEndTimer -= Time.deltaTime;
            if (roundEndTimer <= 0f)
            {
                RoundEvents.ReportRoundEnded(RoundOutcome.PlayerWon);
            }
        }

        /// <summary>
        /// Puts the opponent back on its feet at full health. There is no round
        /// flow yet, so this exists purely so a fight can be retried without
        /// leaving Play mode. Harmless to keep, but it is the debug path, not the
        /// real one.
        /// </summary>
        public void Revive()
        {
            IsKnockedOut = false;
            roundEndTimer = 0f;

            health.ResetHealth();
            motor.Teleport(spawnPosition);
            SetFightingComponents(true);

            Revived?.Invoke();
        }

        /// <summary>
        /// Switches the opponent between fighting and being a ragdoll. The motor
        /// stays enabled so gravity and knockback still apply to the corpse --
        /// only the parts that make decisions go away.
        /// </summary>
        private void SetFightingComponents(bool fighting)
        {
            if (brain != null)
            {
                brain.enabled = fighting;
            }

            if (attack != null)
            {
                attack.enabled = fighting;
            }

            if (!fighting)
            {
                motor.MoveX = 0f;
            }
            else if (body != null)
            {
                body.angularVelocity = 0f;
            }
        }
    }
}
