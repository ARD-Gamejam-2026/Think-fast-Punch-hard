using UnityEngine;

namespace ThinkFast.Economy
{
    /// <summary>
    /// THE boundary between the puzzle half and the fighter half.
    ///
    /// The puzzle system decides two things and nothing else: that a riddle was
    /// solved, and how much Flow that solve was worth (it is the riddle that
    /// knows how fast it was answered). Everything after that -- the AP cap,
    /// whether Flow is currently accepted at all, drain rates, the Flow state --
    /// is the fighter's business and is decided here.
    /// </summary>
    public interface IRiddleRewardSink
    {
        /// <summary>
        /// One solved riddle. Always grants a single Action Point; the Flow
        /// reward may be reduced or ignored entirely depending on the fighter's
        /// state, so callers must not assume it was applied in full.
        /// </summary>
        /// <param name="flowReward">Flow earned by this solve, scaled by how quickly it was answered.</param>
        void GrantSolve(float flowReward);
    }

    /// <summary>
    /// Static hand-off point, so the puzzle half needs no scene reference to the
    /// fighter and no wiring in the Inspector. The entire integration is one line:
    ///
    ///     RiddleRewards.GrantSolve(flowEarned);
    ///
    /// It is safe to call when no fighter exists yet -- the call is simply
    /// dropped, so the puzzle half can be developed and run on its own.
    /// </summary>
    public static class RiddleRewards
    {
        public static IRiddleRewardSink Sink { get; private set; }

        public static bool HasSink => Sink != null;

        public static void Register(IRiddleRewardSink sink)
        {
            Sink = sink;
        }

        public static void Unregister(IRiddleRewardSink sink)
        {
            // Only clear if it is still ours: a newly spawned fighter may already
            // have registered before the old one is torn down.
            if (ReferenceEquals(Sink, sink))
            {
                Sink = null;
            }
        }

        /// <summary>Report a solved riddle. Does nothing if no fighter is listening.</summary>
        public static void GrantSolve(float flowReward)
        {
            Sink?.GrantSolve(flowReward);
        }

        // Static state survives entering Play mode when domain reloading is
        // switched off, which would leave a stale sink from the previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Sink = null;
        }
    }
}
