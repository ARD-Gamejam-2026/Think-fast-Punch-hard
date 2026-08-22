using System;
using ThinkFast.Combat;
using ThinkFast.Rounds;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// What a knocked-out player means for the fight. The mirror of
    /// <see cref="ThinkFast.Enemy.EnemyKnockout"/>, reporting the other outcome.
    ///
    /// This replaces the respawn that used to live in
    /// the hit reaction. Respawning was a placeholder for having
    /// no round flow at all, and it directly contradicts one: a fighter that
    /// stands back up 1.5 seconds later has not lost anything.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerKnockout : MonoBehaviour, IFighterKnockout
    {
        [Tooltip("Seconds between the killing blow and the round being reported over. Long enough for the final knockback to play out -- ending the round on the exact frame of the hit throws away the best-looking moment in the fight.")]
        [SerializeField] private float roundEndDelay = 1.2f;

        private Health health;
        private PlayerController controller;
        private PlayerAttack attack;
        private PlayerInputReader input;
        private Rigidbody2D body;

        private Vector2 spawnPosition;
        private float roundEndTimer;

        /// <summary>Raised the moment health hits zero, before the round-end delay.</summary>
        public event Action KnockedOut;

        /// <summary>Raised when the player is put back on their feet. Debug affordance only.</summary>
        public event Action Revived;

        public bool IsKnockedOut { get; private set; }

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            attack = GetComponent<PlayerAttack>();
            input = GetComponent<PlayerInputReader>();
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
                RoundEvents.ReportRoundEnded(RoundOutcome.PlayerLost);
            }
        }

        /// <summary>
        /// Puts the player back on their feet at full health. Debug affordance,
        /// kept symmetrical with the opponent so a fight can be retried without
        /// leaving Play mode. The round itself does not reopen -- reload the
        /// scene for that.
        /// </summary>
        public void Revive()
        {
            IsKnockedOut = false;
            roundEndTimer = 0f;

            health.ResetHealth();
            transform.position = spawnPosition;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            SetFightingComponents(true);
            Revived?.Invoke();
        }

        /// <summary>
        /// Switches the player between fighting and being a ragdoll. The
        /// controller stays enabled so gravity and knockback still apply to the
        /// body; taking the input reader away is what actually removes control,
        /// and it leaves the movement code none the wiser.
        /// </summary>
        private void SetFightingComponents(bool fighting)
        {
            if (input != null)
            {
                input.enabled = fighting;
            }

            if (attack != null)
            {
                attack.enabled = fighting;
            }

            if (!fighting && controller != null)
            {
                // Whatever the last swing set is now permanent unless it is
                // cleared here -- PlayerAttack is switched off and will not get
                // another step to restore it.
                controller.MoveControlScale = 1f;
                controller.FacingLocked = false;
            }
        }
    }
}
