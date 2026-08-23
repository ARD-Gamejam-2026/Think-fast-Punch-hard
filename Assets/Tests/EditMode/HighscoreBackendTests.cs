using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ThinkFast.Stats;

namespace ThinkFast.Stats.Tests
{
    public class HighscoreBackendTests
    {
        [Test]
        public async Task Memory_backend_returns_uploaded_records_fastest_first()
        {
            var backend = new MemoryBackend();
            await backend.UploadAsync(new MatchRecord { playerName = "slow", startedAt = 0, finishedAt = 30000 });
            await backend.UploadAsync(new MatchRecord { playerName = "fast", startedAt = 0, finishedAt = 10000 });

            IReadOnlyList<MatchRecord> top = await backend.FetchTopAsync(10);

            Assert.AreEqual(2, top.Count);
            Assert.AreEqual("fast", top[0].playerName);
            Assert.AreEqual("slow", top[1].playerName);
        }

        [Test]
        public async Task Memory_backend_fetch_top_limits_the_count()
        {
            var backend = new MemoryBackend();
            await backend.UploadAsync(new MatchRecord { startedAt = 0, finishedAt = 1000 });
            await backend.UploadAsync(new MatchRecord { startedAt = 0, finishedAt = 2000 });
            await backend.UploadAsync(new MatchRecord { startedAt = 0, finishedAt = 3000 });

            IReadOnlyList<MatchRecord> top = await backend.FetchTopAsync(2);

            Assert.AreEqual(2, top.Count);
        }

        [Test]
        public void Rest_backend_builds_the_collection_url_with_one_slash()
        {
            Assert.AreEqual(
                "https://db.firebaseio.com/highscores.json",
                RestFirebaseBackend.BuildCollectionUrl("https://db.firebaseio.com/"));
            Assert.AreEqual(
                "https://db.firebaseio.com/highscores.json",
                RestFirebaseBackend.BuildCollectionUrl("https://db.firebaseio.com"));
        }

        [Test]
        public void Rest_backend_serializes_the_record_fields()
        {
            string json = RestFirebaseBackend.SerializeRecord(
                new MatchRecord { playerName = "p", damageDealt = 42, endHealth = 30 });

            StringAssert.Contains("\"playerName\":\"p\"", json);
            StringAssert.Contains("\"damageDealt\":42", json);
            StringAssert.Contains("\"endHealth\":30", json);
        }

        [Test]
        public void Rest_backend_parses_a_realtime_database_collection_response()
        {
            string response =
                "{\"-Na\":{\"playerName\":\"a\",\"finishedAt\":20000}," +
                "\"-Nb\":{\"playerName\":\"b\",\"finishedAt\":5000}}";

            List<MatchRecord> records = RestFirebaseBackend.ParseCollection(response);

            Assert.AreEqual(2, records.Count);
            MatchRecord a = records.Find(r => r.playerName == "a");
            MatchRecord b = records.Find(r => r.playerName == "b");
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreEqual(20000, a.finishedAt);
            Assert.AreEqual(5000, b.finishedAt);
        }

        [Test]
        public void Rest_backend_parses_an_empty_response_as_no_records()
        {
            Assert.AreEqual(0, RestFirebaseBackend.ParseCollection("null").Count);
            Assert.AreEqual(0, RestFirebaseBackend.ParseCollection("").Count);
        }

        [Test]
        public void Rest_backend_parses_a_player_name_containing_braces()
        {
            string response =
                "{\"-Na\":{\"playerName\":\"a{b}c\",\"finishedAt\":5000}}";

            List<MatchRecord> records = RestFirebaseBackend.ParseCollection(response);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("a{b}c", records[0].playerName);
            Assert.AreEqual(5000, records[0].finishedAt);
        }

        [Test]
        public void Rest_backend_parses_a_player_name_with_an_escaped_quote()
        {
            string response =
                "{\"-Na\":{\"playerName\":\"a\\\"b\",\"finishedAt\":1}}";

            List<MatchRecord> records = RestFirebaseBackend.ParseCollection(response);

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("a\"b", records[0].playerName);
        }
    }
}
