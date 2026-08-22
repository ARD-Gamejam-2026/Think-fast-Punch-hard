using System;
using UnityEngine;

namespace ThinkFast.Rounds
{
    public enum RoundOutcome
    {
        PlayerWon,
        PlayerLost,
    }

    /// <summary>
    /// The seam between "a fighter was knocked out" and "the round is over".
    ///
    /// Deliberately a static hand-off, in the same shape as
    /// <see cref="ThinkFast.Economy.RiddleRewards"/>: whoever lands the killing
    /// blow should not need a scene reference to a match manager that does not
    /// exist yet, and the fight must keep running standalone when nothing is
    /// listening.
    ///
    /// Today the only listener is a debug banner. When there is a real match flow
    /// -- scene transitions, a results screen, best-of-three -- it subscribes
    /// here and nothing in combat has to change.
    /// </summary>
    public static class RoundEvents
    {
        /// <summary>
        /// Raised exactly once per round. Listeners should assume the fight is
        /// over: fighters may still be mid-animation, but no further outcome is
        /// coming.
        /// </summary>
        public static event Action<RoundOutcome> RoundEnded;

        public static bool IsRoundOver { get; private set; }

        /// <summary>
        /// Reports the round as decided. The FIRST report wins and later ones are
        /// dropped -- a double KO in the same physics step must not fire two
        /// contradictory endings, and a knocked-out fighter can plausibly report
        /// more than once as its corpse is hit again.
        /// </summary>
        public static void ReportRoundEnded(RoundOutcome outcome)
        {
            if (IsRoundOver)
            {
                return;
            }

            IsRoundOver = true;
            RoundEnded?.Invoke(outcome);
        }

        /// <summary>Reopens the round. Call before restarting a fight in place.</summary>
        public static void ResetRound()
        {
            IsRoundOver = false;
        }

        // Static state survives entering Play mode when domain reloading is
        // switched off, which would leave a stale subscriber list and a round
        // that is already over before it starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            RoundEnded = null;
            IsRoundOver = false;
        }
    }
}
