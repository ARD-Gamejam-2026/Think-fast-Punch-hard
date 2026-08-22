using System.Text;
using ThinkFast.Combat;
using ThinkFast.Player;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// Shouts when the opponent has been tuned into a state it cannot work in.
    ///
    /// Most of the AI adapts to its own settings: reachability is derived from
    /// the motor's jump apex, and a swing is aimed using the attack's real frame
    /// data. But a handful of numbers are only correct *relative to each other*,
    /// and nothing in the code notices when that stops being true. Standing
    /// further away than you can punch is not a crash, it is an opponent that
    /// walks up and then politely does nothing.
    ///
    /// Everything here is a warning, never a correction. Values stay exactly as
    /// they were set -- the whole point is to be able to tune freely and be told
    /// what that broke.
    ///
    /// Editor and development builds only; it compiles to nothing in a release
    /// player.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(EnemyAttack))]
    public sealed class EnemyTuningCheck : MonoBehaviour
    {
        [Tooltip("Also log the derived reach figures even when nothing is wrong. Useful when laying out a stage: the numbers tell you how far apart platforms may be.")]
        [SerializeField] private bool alwaysReportReach;

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Run();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Run()
        {
            var motor = GetComponent<EnemyMotor>();
            var brain = GetComponent<EnemyBrain>();
            var attack = GetComponent<EnemyAttack>();

            var problems = new StringBuilder();
            float usableRise = brain.UsableRise;

            // 1. Can it climb at all? The margin is subtracted from the apex, so
            //    a generous margin on a small jump leaves nothing.
            if (usableRise <= 0f)
            {
                problems.AppendLine(
                    $"- Usable rise is {usableRise:0.00}: jumpReachMargin is eating the whole jump. " +
                    "It will never climb anything and will read as Stranded the moment you stand on a platform.");
            }

            // 2. Can it follow the player? Anywhere the player can jump to, the
            //    opponent has to be able to jump to as well -- otherwise there is
            //    a ledge somewhere that is simply safe.
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null && player.JumpApexHeight > usableRise)
            {
                problems.AppendLine(
                    $"- The player jumps {player.JumpApexHeight:0.00} but the opponent can only land on surfaces " +
                    $"{usableRise:0.00} up. Any platform reachable by the player in one jump and not by the " +
                    "opponent is a safe camping spot. Raise the opponent's jumpHeight above the player's.");
            }

            // 3. Does it stand inside its own reach? preferredDistance is where it
            //    settles; attackRange is how far it will swing from. Settling
            //    outside that means it closes, stops, and never attacks.
            if (brain.PreferredDistance > brain.AttackRange)
            {
                problems.AppendLine(
                    $"- preferredDistance ({brain.PreferredDistance:0.00}) is outside attackRange " +
                    $"({brain.AttackRange:0.00}). It will hold a distance it cannot punch from.");
            }

            // 4. Does attackRange match the hitbox the ground attack aims? The
            //    two are set independently and nothing reconciles them.
            //
            //    Only the ground attack is checked, and the reason is worth
            //    knowing: its moveControlScale is 0, so the fighter does not move
            //    at all during startup and the swing has to connect from exactly
            //    where it was standing. The air attack is always closing, and the
            //    prediction in TryAttack accounts for that -- starting an air
            //    swing from beyond the hitbox is correct, not a mistake.
            CheckGroundReach(problems, attack.GroundAttack, brain.AttackRange);

            // 5. Boarding a platform means stopping on a spot. If it cannot stop
            //    within the margin kept clear of the platform edges, hops start
            //    launching from beyond the edge and miss.
            float drift = motor.StoppingDistance;
            if (drift > brain.ClimbEdgeInset)
            {
                problems.AppendLine(
                    $"- It drifts {drift:0.00} after being told to stop, but only {brain.ClimbEdgeInset:0.00} " +
                    "is kept clear of platform edges (climbEdgeInset). Boarding jumps will launch from past " +
                    "the edge. Raise climbEdgeInset, or raise groundDeceleration.");
            }

            if (problems.Length > 0)
            {
                Debug.LogWarning($"{nameof(EnemyTuningCheck)} on '{name}':\n{problems}\n{ReachReport(motor, usableRise)}", this);
            }
            else if (alwaysReportReach)
            {
                Debug.Log($"{nameof(EnemyTuningCheck)} on '{name}': settings look consistent.\n{ReachReport(motor, usableRise)}", this);
            }
        }

        private static void CheckGroundReach(StringBuilder problems, AttackDefinition definition, float attackRange)
        {
            // Furthest point the hitbox actually covers, measured from the
            // fighter's own centre.
            float hitboxReach = definition.hitboxOffset.x + (definition.hitboxSize.x * 0.5f);

            if (attackRange > hitboxReach + 0.05f)
            {
                problems.AppendLine(
                    $"- attackRange ({attackRange:0.00}) is longer than the ground hitbox reaches " +
                    $"({hitboxReach:0.00}), and the ground attack roots the fighter during startup. " +
                    "It will start swings from where they cannot connect.");
            }
        }

        /// <summary>
        /// The numbers a stage layout has to respect. Printed rather than
        /// enforced -- level geometry is not this component's business, but
        /// nothing else in the project can work these out either.
        /// </summary>
        private static string ReachReport(EnemyMotor motor, float usableRise)
        {
            var report = new StringBuilder();
            report.AppendLine($"  Reach: it can land on surfaces up to {usableRise:0.00} above the one it is standing on.");
            report.AppendLine("  Widest crossable gap, by how much height the far side gains (running launch):");

            for (float rise = 0f; rise <= usableRise + 0.001f; rise += Mathf.Max(0.5f, usableRise / 4f))
            {
                report.AppendLine($"    +{rise:0.0} up  ->  {motor.HorizontalJumpDistance(rise):0.00} across");
            }

            report.Append("  A hop launched from a standstill covers appreciably less than these.");
            return report.ToString();
        }
#endif
    }
}
