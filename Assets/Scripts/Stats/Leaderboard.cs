using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Pure ranking and formatting for the highscore view. Kept free of
    /// UnityEngine so it can be unit-tested without the editor.
    /// </summary>
    public static class Leaderboard
    {
        /// <summary>
        /// The leaderboard: one row per player — their fastest winning run —
        /// ordered by <see cref="MatchRecord.DurationMillis"/> ascending and
        /// capped at <paramref name="topN"/>. Losses and null entries are dropped,
        /// so pure duration ranking never puts a quick loss above a real win, and
        /// a player never appears twice. Players are grouped by exact name.
        /// </summary>
        public static IReadOnlyList<MatchRecord> RankWins(IEnumerable<MatchRecord> records, int topN)
        {
            if (records == null)
            {
                return new List<MatchRecord>();
            }

            return records
                .Where(record => record != null && record.PlayerWon())
                .GroupBy(record => record.playerName ?? string.Empty)
                .Select(group => group.OrderBy(record => record.DurationMillis()).First())
                .OrderBy(record => record.DurationMillis())
                .Take(Math.Max(0, topN))
                .ToList();
        }

        /// <summary>
        /// Formats a duration in milliseconds as <c>m:ss.d</c> (minutes, then
        /// zero-padded seconds, then tenths). Negative values clamp to zero.
        /// </summary>
        public static string FormatDuration(long millis)
        {
            if (millis < 0)
            {
                millis = 0;
            }

            long totalTenths = millis / 100;
            long minutes = totalTenths / 600;
            long seconds = (totalTenths / 10) % 60;
            long tenths = totalTenths % 10;
            return $"{minutes}:{seconds:00}.{tenths}";
        }
    }
}
