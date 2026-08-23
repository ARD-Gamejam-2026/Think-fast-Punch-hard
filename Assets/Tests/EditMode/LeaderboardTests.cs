using System.Collections.Generic;
using NUnit.Framework;
using ThinkFast.Stats;

namespace ThinkFast.Stats.Tests
{
    public class LeaderboardTests
    {
        [Test]
        public void Player_won_is_derived_from_the_end_healths()
        {
            Assert.IsTrue(new MatchRecord { endHealth = 30, endOpponentHealth = 0 }.PlayerWon());
            Assert.IsFalse(new MatchRecord { endHealth = 0, endOpponentHealth = 25 }.PlayerWon());
            Assert.IsFalse(new MatchRecord { endHealth = 10, endOpponentHealth = 10 }.PlayerWon());
        }

        [Test]
        public void Rank_wins_drops_losses_and_orders_fastest_first()
        {
            var records = new List<MatchRecord>
            {
                new MatchRecord { playerName = "fastLoss", startedAt = 0, finishedAt = 1000, endHealth = 0, endOpponentHealth = 20 },
                new MatchRecord { playerName = "slowWin", startedAt = 0, finishedAt = 30000, endHealth = 15, endOpponentHealth = 0 },
                new MatchRecord { playerName = "fastWin", startedAt = 0, finishedAt = 10000, endHealth = 5, endOpponentHealth = 0 },
            };

            IReadOnlyList<MatchRecord> ranked = Leaderboard.RankWins(records, 10);

            Assert.AreEqual(2, ranked.Count);
            Assert.AreEqual("fastWin", ranked[0].playerName);
            Assert.AreEqual("slowWin", ranked[1].playerName);
        }

        [Test]
        public void Rank_wins_truncates_to_the_top_count()
        {
            var records = new List<MatchRecord>
            {
                new MatchRecord { playerName = "slowWin", startedAt = 0, finishedAt = 30000, endHealth = 15, endOpponentHealth = 0 },
                new MatchRecord { playerName = "fastWin", startedAt = 0, finishedAt = 10000, endHealth = 5, endOpponentHealth = 0 },
            };

            IReadOnlyList<MatchRecord> ranked = Leaderboard.RankWins(records, 1);

            Assert.AreEqual(1, ranked.Count);
            Assert.AreEqual("fastWin", ranked[0].playerName);
        }

        [Test]
        public void Rank_wins_keeps_one_row_per_player_at_their_best_time()
        {
            var records = new List<MatchRecord>
            {
                new MatchRecord { playerName = "Marcel", startedAt = 0, finishedAt = 46800, endHealth = 10, endOpponentHealth = 0 },
                new MatchRecord { playerName = "Marcel", startedAt = 0, finishedAt = 42000, endHealth = 10, endOpponentHealth = 0 },
                new MatchRecord { playerName = "Hehe", startedAt = 0, finishedAt = 47200, endHealth = 10, endOpponentHealth = 0 },
            };

            IReadOnlyList<MatchRecord> ranked = Leaderboard.RankWins(records, 10);

            Assert.AreEqual(2, ranked.Count);
            Assert.AreEqual("Marcel", ranked[0].playerName);
            Assert.AreEqual(42000, ranked[0].DurationMillis());
            Assert.AreEqual("Hehe", ranked[1].playerName);
        }

        [Test]
        public void Rank_wins_handles_a_null_input()
        {
            Assert.AreEqual(0, Leaderboard.RankWins(null, 5).Count);
        }

        [Test]
        public void Format_duration_renders_minutes_seconds_and_tenths()
        {
            Assert.AreEqual("0:42.3", Leaderboard.FormatDuration(42300));
            Assert.AreEqual("1:05.5", Leaderboard.FormatDuration(65500));
            Assert.AreEqual("12:34.0", Leaderboard.FormatDuration(754000));
        }

        [Test]
        public void Format_duration_clamps_a_negative_value_to_zero()
        {
            Assert.AreEqual("0:00.0", Leaderboard.FormatDuration(-100));
        }
    }
}
