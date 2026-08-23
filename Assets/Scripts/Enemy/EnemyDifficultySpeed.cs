using ThinkFast.Quiz;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// Ramps the opponent's top run speed with match difficulty: slow at the
    /// start of a round, fast late-game. Reads the shared normalized difficulty
    /// from <see cref="DifficultyRamp.Current01"/> -- the one value both halves of
    /// the game agree on -- and maps it onto the motor's top speed.
    ///
    /// Deletable: remove it and the motor keeps its serialized maxRunSpeed, so the
    /// fighter still runs standalone.
    ///
    /// Needs a <see cref="DifficultyRamp"/> in the scene to do anything but slow the
    /// opponent down: with no ramp, <see cref="DifficultyRamp.Current01"/> stays 0 and
    /// this pins the top speed at <c>slowStartSpeed</c> forever -- below the motor's own
    /// serialized default. If you want the ramp, add a DifficultyRamp; if you do not,
    /// remove this component rather than leaving it to hold the opponent slow.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor))]
    public sealed class EnemyDifficultySpeed : MonoBehaviour
    {
        [Tooltip("Left empty, uses the EnemyMotor on this object.")]
        [SerializeField] private EnemyMotor motor;

        [Tooltip("Top run speed at difficulty 0 (round start). Keep high enough for the opponent to traverse the stage -- it also sets the running-jump reach early on.")]
        [SerializeField] private float slowStartSpeed = 4.5f;

        [Tooltip("Top run speed at difficulty 1 (late game). Kept under the player's 9 so the opponent cannot always close the gap.")]
        [SerializeField] private float fastCapSpeed = 8f;

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<EnemyMotor>();
            }
        }

        private void Update()
        {
            if (motor == null)
            {
                return;
            }
            motor.SetMaxRunSpeed(Mathf.Lerp(slowStartSpeed, fastCapSpeed, DifficultyRamp.Current01));
        }
    }
}
