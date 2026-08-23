using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Where finished-round records are stored and read back. Abstracts Firebase
    /// so the game runs against an in-memory store until a database is wired up.
    /// </summary>
    public interface IHighscoreBackend
    {
        /// <summary>Stores one finished-round record.</summary>
        Task UploadAsync(MatchRecord record);

        /// <summary>Reads back the fastest-winning records, up to a count.</summary>
        Task<IReadOnlyList<MatchRecord>> FetchTopAsync(int count);
    }
}
