using ThinkFast.Enemy;
using UnityEngine;

namespace ThinkFast.Tutorial
{
    /// <summary>
    /// Turns the opponent into a training dummy: it stands there, it takes the
    /// hit, and it gets back up.
    ///
    /// Standing still is done by switching off the two components that make
    /// decisions -- the brain and the attack -- and leaving the motor alone, so
    /// gravity and knockback still read exactly as they do in a real fight. That
    /// is the whole point of practising against it.
    ///
    /// Getting back up matters more than it looks. A knocked-out opponent
    /// reports the round over, and the tutorial has no round to end; without
    /// this the dummy would lie down for good the first time a Flow-state punch
    /// finished it, and the punching step would have nothing left to punch. The
    /// revive happens inside the knockout's own report delay, so no round-ended
    /// report is ever made.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyKnockout))]
    public sealed class TutorialDummy : MonoBehaviour
    {
        [Tooltip("Seconds the dummy stays down before standing back up. Must be shorter than the knockout's own round-end delay, or the tutorial reports a round it is not running.")]
        [SerializeField, Min(0f)] private float reviveDelaySeconds = 0.8f;

        [Tooltip("Optional. The dummy turns to face this, so it reads as a sparring partner rather than a statue. It still never moves and never swings.")]
        [SerializeField] private Transform faceTarget;

        private EnemyKnockout knockout;
        private EnemyMotor motor;
        private float reviveTimer;

        private void Awake()
        {
            knockout = GetComponent<EnemyKnockout>();
            motor = GetComponent<EnemyMotor>();
            StandDown();
        }

        private void OnEnable()
        {
            knockout.KnockedOut += HandleKnockedOut;
        }

        private void OnDisable()
        {
            knockout.KnockedOut -= HandleKnockedOut;
        }

        private void HandleKnockedOut()
        {
            reviveTimer = reviveDelaySeconds;
        }

        private void Update()
        {
            FaceTheTarget();

            if (reviveTimer <= 0f)
            {
                return;
            }

            reviveTimer -= Time.deltaTime;
            if (reviveTimer > 0f)
            {
                return;
            }

            // Revive puts the brain and the attack back, which is the one thing
            // a dummy must not have -- so they go straight back off.
            knockout.Revive();
            StandDown();
        }

        /// <summary>
        /// Turns on the spot to look at whoever is hitting it. Facing is the one
        /// thing the dummy is allowed to change, because a fighter punched in the
        /// back with no reaction at all reads as broken rather than as passive.
        /// </summary>
        private void FaceTheTarget()
        {
            if (faceTarget == null || motor == null || knockout.IsKnockedOut)
            {
                return;
            }

            float offset = faceTarget.position.x - transform.position.x;
            if (Mathf.Abs(offset) < 0.2f)
            {
                return;
            }

            motor.SetFacing(Mathf.Sign(offset));
        }

        /// <summary>
        /// Removes everything that would make the dummy fight back, and nothing
        /// else.
        /// </summary>
        private void StandDown()
        {
            var brain = GetComponent<EnemyBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            var attack = GetComponent<EnemyAttack>();
            if (attack != null)
            {
                attack.enabled = false;
            }

            if (motor != null)
            {
                motor.MoveX = 0f;
            }
        }
    }
}
