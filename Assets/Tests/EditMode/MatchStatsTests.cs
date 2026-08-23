using System;
using NUnit.Framework;
using ThinkFast.Stats;

namespace ThinkFast.Stats.Tests
{
    public class MatchStatsTests
    {
        [SetUp]
        public void Reset()
        {
            MatchStats.Clock = () => 0.0;
            MatchStats.Begin();
        }

        [TearDown]
        public void Restore()
        {
            MatchStats.ResetForTests();
        }

        [Test]
        public void Begin_zeros_the_quiz_counters()
        {
            Assert.AreEqual(0, MatchStats.QuizzesSolved);
            Assert.AreEqual(0, MatchStats.QuizzesRight);
            Assert.AreEqual(0, MatchStats.QuizzesWrong);
            Assert.AreEqual(0, MatchStats.QuizzesTimedOut);
        }

        [Test]
        public void Recording_results_bumps_the_matching_counter_and_solved()
        {
            MatchStats.RecordCorrect();
            MatchStats.RecordCorrect();
            MatchStats.RecordWrong();
            MatchStats.RecordTimedOut();

            Assert.AreEqual(4, MatchStats.QuizzesSolved);
            Assert.AreEqual(2, MatchStats.QuizzesRight);
            Assert.AreEqual(1, MatchStats.QuizzesWrong);
            Assert.AreEqual(1, MatchStats.QuizzesTimedOut);
        }

        [Test]
        public void Time_to_beat_opponent_is_end_minus_start()
        {
            double now = 10.0;
            MatchStats.Clock = () => now;
            MatchStats.Begin();
            now = 42.5;
            MatchStats.Finish(true);

            Assert.AreEqual(32.5, MatchStats.TimeToBeatOpponent, 0.0001);
        }
    }
}
