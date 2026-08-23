using NUnit.Framework;
using ThinkFast.Stats;

namespace ThinkFast.Stats.Tests
{
    public class MatchStatsTests
    {
        [SetUp]
        public void Reset()
        {
            MatchStats.Clock = () => 0L;
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
            Assert.AreEqual(0, MatchStats.QuizzesRight);
            Assert.AreEqual(0, MatchStats.QuizzesWrong);
            Assert.AreEqual(0, MatchStats.QuizzesTimedOut);
        }

        [Test]
        public void Recording_results_bumps_the_matching_counter()
        {
            MatchStats.RecordCorrect();
            MatchStats.RecordCorrect();
            MatchStats.RecordWrong();
            MatchStats.RecordTimedOut();

            Assert.AreEqual(2, MatchStats.QuizzesRight);
            Assert.AreEqual(1, MatchStats.QuizzesWrong);
            Assert.AreEqual(1, MatchStats.QuizzesTimedOut);
        }

        [Test]
        public void Duration_is_finish_minus_start()
        {
            long now = 10000;
            MatchStats.Clock = () => now;
            MatchStats.Begin();
            now = 42500;
            MatchStats.Finish();

            MatchRecord record = MatchStats.Snapshot();
            Assert.AreEqual(10000, record.startedAt);
            Assert.AreEqual(42500, record.finishedAt);
            Assert.AreEqual(32500, record.DurationMillis());
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
            MatchStats.Clock = () => 5000L;
            MatchStats.Begin();
            MatchStats.RecordCorrect();
            MatchStats.RecordTimedOut();
            MatchStats.SetPlayerHealth(100);
            MatchStats.SetPlayerHealth(60);
            MatchStats.SetOpponentHealth(100);
            MatchStats.SetOpponentHealth(0);
            MatchStats.Clock = () => 12000L;
            MatchStats.Finish();

            MatchRecord record = MatchStats.Snapshot();

            Assert.AreEqual(5000, record.startedAt);
            Assert.AreEqual(12000, record.finishedAt);
            Assert.AreEqual(7000, record.DurationMillis());
            Assert.AreEqual(1, record.quizzesRight);
            Assert.AreEqual(1, record.quizzesTimedOut);
            Assert.AreEqual(40, record.damageTaken);
            Assert.AreEqual(100, record.damageDealt);
            Assert.AreEqual(60, record.endHealth);
            Assert.AreEqual(0, record.endOpponentHealth);
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

            Assert.AreEqual(1, MatchStats.QuizzesRight);
            Assert.AreEqual(0, MatchStats.QuizzesWrong);
            Assert.AreEqual(100, MatchStats.EndHealth);
            Assert.AreEqual(0, MatchStats.DamageTaken);
            Assert.AreEqual(0, MatchStats.EndOpponentHealth);
        }
    }
}
