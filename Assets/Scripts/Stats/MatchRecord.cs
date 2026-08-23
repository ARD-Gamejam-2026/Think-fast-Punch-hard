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
        public float timeToBeatOpponent;
        public int quizzesSolved;
        public int quizzesRight;
        public int quizzesWrong;
        public int quizzesTimedOut;
        public int damageDealt;
        public int damageTaken;
        public int endHealth;
        public int endOpponentHealth;
        public string playerName;
        public string finishedAt;
    }
}
