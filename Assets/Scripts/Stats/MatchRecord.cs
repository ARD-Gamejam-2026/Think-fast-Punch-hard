using System;

namespace ThinkFast.Stats
{
    /// <summary>
    /// One finished round's statistics, in the exact shape uploaded to Firebase.
    /// The public field names are the stored JSON keys, so they must not be
    /// renamed.
    /// </summary>
    [Serializable]
    public class MatchRecord
    {
        public long startedAt;
        public long finishedAt;
        public int quizzesRight;
        public int quizzesWrong;
        public int quizzesTimedOut;
        public int damageDealt;
        public int damageTaken;
        public int endHealth;
        public int endOpponentHealth;
        public string playerName;

        /// <summary>Round duration in milliseconds, derived from the timestamps.</summary>
        public long DurationMillis()
        {
            return finishedAt - startedAt;
        }

        /// <summary>
        /// Whether the player won, derived from the end healths (the loser is
        /// knocked out to zero). Not stored, computed at display time.
        /// </summary>
        public bool PlayerWon()
        {
            return endHealth > endOpponentHealth;
        }
    }
}
