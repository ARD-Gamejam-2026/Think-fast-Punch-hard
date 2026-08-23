using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Keeps records in memory. Used by tests and as the fallback when no
    /// database URL is configured, so uploading never breaks the game.
    /// </summary>
    public sealed class MemoryBackend : IHighscoreBackend
    {
        private readonly List<MatchRecord> records = new List<MatchRecord>();

        /// <summary>Stores the record in the in-memory list.</summary>
        public Task UploadAsync(MatchRecord record)
        {
            records.Add(record);
            return Task.CompletedTask;
        }

        /// <summary>Returns the fastest records first, up to the count.</summary>
        public Task<IReadOnlyList<MatchRecord>> FetchTopAsync(int count)
        {
            IReadOnlyList<MatchRecord> top = records
                .OrderBy(record => record.timeToBeatOpponent)
                .Take(count)
                .ToList();
            return Task.FromResult(top);
        }
    }
}
