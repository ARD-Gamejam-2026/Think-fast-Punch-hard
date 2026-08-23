using System;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// The match-statistics counter: a static accumulator the quiz and fighters
    /// write into during a round, snapshotted for upload when the round ends.
    /// Same static hand-off shape as <c>RoundEvents</c> and <c>MatchResult</c> —
    /// no scene wiring, and it survives the fight-to-end scene load so the end
    /// screen can read it back.
    /// </summary>
    public static class MatchStats
    {
        /// <summary>Time source in seconds. Swapped by tests for a fixed clock.</summary>
        public static Func<double> Clock = DefaultClock;

        /// <summary>Number of quiz questions solved this round (right, wrong and timed out).</summary>
        public static int QuizzesSolved { get; private set; }

        /// <summary>Number of quiz questions answered right this round.</summary>
        public static int QuizzesRight { get; private set; }

        /// <summary>Number of quiz questions answered wrong this round.</summary>
        public static int QuizzesWrong { get; private set; }

        /// <summary>Number of quiz questions that timed out this round.</summary>
        public static int QuizzesTimedOut { get; private set; }

        /// <summary>Total damage dealt to the opponent this round.</summary>
        public static int DamageDealt { get; private set; }

        /// <summary>Total damage taken by the player this round.</summary>
        public static int DamageTaken { get; private set; }

        /// <summary>The player's health at the end of the round.</summary>
        public static int EndHealth { get; private set; }

        /// <summary>The opponent's health at the end of the round.</summary>
        public static int EndOpponentHealth { get; private set; }

        /// <summary>Whether the player won the round.</summary>
        public static bool PlayerWon { get; private set; }

        /// <summary>Whether a round has finished and left a record to upload.</summary>
        public static bool HasFinished { get; private set; }

        /// <summary>Seconds from <see cref="Begin"/> to <see cref="Finish"/>.</summary>
        public static double TimeToBeatOpponent
        {
            get { return endTime - startTime; }
        }

        private static double startTime;
        private static double endTime;
        private static bool hasPlayerBaseline;
        private static bool hasOpponentBaseline;
        private static int lastPlayerHealth;
        private static int lastOpponentHealth;
        private static string finishedAtIso = string.Empty;

        /// <summary>Starts a fresh round: stamps the start time and zeros counters.</summary>
        public static void Begin()
        {
            startTime = Clock();
            endTime = startTime;
            QuizzesSolved = 0;
            QuizzesRight = 0;
            QuizzesWrong = 0;
            QuizzesTimedOut = 0;
            DamageDealt = 0;
            DamageTaken = 0;
            EndHealth = 0;
            EndOpponentHealth = 0;
            PlayerWon = false;
            HasFinished = false;
            hasPlayerBaseline = false;
            hasOpponentBaseline = false;
            lastPlayerHealth = 0;
            lastOpponentHealth = 0;
            finishedAtIso = string.Empty;
        }

        /// <summary>Records a correct answer, also counting it as solved.</summary>
        public static void RecordCorrect()
        {
            // Once the round is finished the record is frozen at the knockout instant:
            // stats that resolve during the scene-transition delay must not leak in.
            if (HasFinished)
            {
                return;
            }

            QuizzesSolved++;
            QuizzesRight++;
        }

        /// <summary>Records a wrong answer, also counting it as solved.</summary>
        public static void RecordWrong()
        {
            if (HasFinished)
            {
                return;
            }

            QuizzesSolved++;
            QuizzesWrong++;
        }

        /// <summary>Records a timed-out question, also counting it as solved.</summary>
        public static void RecordTimedOut()
        {
            if (HasFinished)
            {
                return;
            }

            QuizzesSolved++;
            QuizzesTimedOut++;
        }

        /// <summary>Finishes the round: stamps the end time and win/loss.</summary>
        public static void Finish(bool playerWon)
        {
            endTime = Clock();
            PlayerWon = playerWon;
            finishedAtIso = DateTime.UtcNow.ToString("o");
            HasFinished = true;
        }

        /// <summary>
        /// Reports the player's current health. A drop since the last report is
        /// added to damage taken; a rise (a future heal) is not counted.
        /// </summary>
        public static void SetPlayerHealth(int current)
        {
            if (HasFinished)
            {
                return;
            }

            if (hasPlayerBaseline && current < lastPlayerHealth)
            {
                DamageTaken += lastPlayerHealth - current;
            }

            lastPlayerHealth = current;
            hasPlayerBaseline = true;
            EndHealth = current;
        }

        /// <summary>
        /// Reports the opponent's current health. A drop since the last report is
        /// added to damage dealt by the player.
        /// </summary>
        public static void SetOpponentHealth(int current)
        {
            if (HasFinished)
            {
                return;
            }

            if (hasOpponentBaseline && current < lastOpponentHealth)
            {
                DamageDealt += lastOpponentHealth - current;
            }

            lastOpponentHealth = current;
            hasOpponentBaseline = true;
            EndOpponentHealth = current;
        }

        /// <summary>Copies the current state into an uploadable record.</summary>
        public static MatchRecord Snapshot()
        {
            return new MatchRecord
            {
                timeToBeatOpponent = (float)TimeToBeatOpponent,
                quizzesSolved = QuizzesSolved,
                quizzesRight = QuizzesRight,
                quizzesWrong = QuizzesWrong,
                quizzesTimedOut = QuizzesTimedOut,
                damageDealt = DamageDealt,
                damageTaken = DamageTaken,
                endHealth = EndHealth,
                endOpponentHealth = EndOpponentHealth,
                playerWon = PlayerWon,
                playerName = string.Empty,
                finishedAt = finishedAtIso,
            };
        }

        private static double DefaultClock()
        {
            return Time.realtimeSinceStartupAsDouble;
        }

        /// <summary>Resets all state and restores the default clock. For tests.</summary>
        public static void ResetForTests()
        {
            Clock = DefaultClock;
            Begin();
        }

        // Static state survives entering Play mode when domain reloading is off,
        // which would carry last session's counters into a new fight.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Clock = DefaultClock;
            Begin();
        }
    }
}
