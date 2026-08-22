using UnityEngine;

namespace ThinkFast.Rounds
{
    /// <summary>
    /// Carries how the fight ended across the scene load into the end screen.
    ///
    /// A scene load destroys everything in the fight, including whoever knew the
    /// outcome, so the one fact the end screen needs has to outlive it. This is
    /// deliberately the smallest thing that can: one enum and a flag, with no
    /// object to find and nothing to wire in the Inspector -- the same static
    /// hand-off shape as <see cref="RoundEvents"/> and
    /// <see cref="ThinkFast.Economy.RiddleRewards"/>.
    ///
    /// It is a *result*, not a save game. Nothing here is persisted to disk and
    /// nothing survives quitting.
    /// </summary>
    public static class MatchResult
    {
        /// <summary>Whether a fight has finished and left a result behind.</summary>
        public static bool HasResult { get; private set; }

        /// <summary>
        /// How the last fight ended. Only meaningful while
        /// <see cref="HasResult"/> is true.
        /// </summary>
        public static RoundOutcome Outcome { get; private set; }

        /// <summary>Records how the fight ended, replacing any earlier result.</summary>
        public static void Record(RoundOutcome outcome)
        {
            Outcome = outcome;
            HasResult = true;
        }

        /// <summary>
        /// Forgets the last result. Called when a fight starts, so the end screen
        /// can tell "this fight is over" from "no fight has happened yet" -- which
        /// is what lets the scene be opened on its own without claiming a win.
        /// </summary>
        public static void Clear()
        {
            HasResult = false;
        }

        // Static state survives entering Play mode when domain reloading is
        // switched off, which would show the end screen last session's result.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            HasResult = false;
            Outcome = RoundOutcome.PlayerWon;
        }
    }
}
