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
            MatchStats.Finish();

            Assert.AreEqual(32.5, MatchStats.TimeToBeatOpponent, 0.0001);
        }

        [Test]
        public void Player_health_decrease_accrues_damage_taken_but_a_heal_does_not()
        {
            MatchStats.SetPlayerHealth(100); // baseline, no damage
            MatchStats.SetPlayerHealth(80);  // -20 taken
            MatchStats.SetPlayerHealth(90);  // heal, ignored
            MatchStats.SetPlayerHealth(70);  // -20 taken

            Assert.AreEqual(40, MatchStats.DamageTaken);
            Assert.AreEqual(70, MatchStats.EndHealth);
        }

        [Test]
        public void Opponent_health_decrease_accrues_damage_dealt()
        {
            MatchStats.SetOpponentHealth(100);
            MatchStats.SetOpponentHealth(55);

            Assert.AreEqual(45, MatchStats.DamageDealt);
            Assert.AreEqual(55, MatchStats.EndOpponentHealth);
        }

        [Test]
        public void Snapshot_reflects_the_accumulated_state()
        {
            MatchStats.Clock = () => 5.0;
            MatchStats.Begin();
            MatchStats.RecordCorrect();
            MatchStats.RecordTimedOut();
            MatchStats.SetPlayerHealth(100);
            MatchStats.SetPlayerHealth(60);
            MatchStats.SetOpponentHealth(100);
            MatchStats.SetOpponentHealth(0);
            MatchStats.Clock = () => 12.0;
            MatchStats.Finish();

            MatchRecord record = MatchStats.Snapshot();

            Assert.AreEqual(7.0f, record.timeToBeatOpponent, 0.0001f);
            Assert.AreEqual(2, record.quizzesSolved);
            Assert.AreEqual(1, record.quizzesRight);
            Assert.AreEqual(1, record.quizzesTimedOut);
            Assert.AreEqual(40, record.damageTaken);
            Assert.AreEqual(100, record.damageDealt);
            Assert.AreEqual(60, record.endHealth);
            Assert.AreEqual(0, record.endOpponentHealth);
            Assert.IsFalse(string.IsNullOrEmpty(record.finishedAt));
        }

        [Test]
        public void Updates_after_finish_are_ignored()
        {
            MatchStats.SetPlayerHealth(100);
            MatchStats.RecordCorrect();
            MatchStats.Finish();

            MatchStats.RecordCorrect();
            MatchStats.RecordWrong();
            MatchStats.SetPlayerHealth(50);
            MatchStats.SetOpponentHealth(10);

            Assert.AreEqual(1, MatchStats.QuizzesSolved);
            Assert.AreEqual(1, MatchStats.QuizzesRight);
            Assert.AreEqual(0, MatchStats.QuizzesWrong);
            Assert.AreEqual(100, MatchStats.EndHealth);
            Assert.AreEqual(0, MatchStats.DamageTaken);
            Assert.AreEqual(0, MatchStats.EndOpponentHealth);
        }
    }
}
