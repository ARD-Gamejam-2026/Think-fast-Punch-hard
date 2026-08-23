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
            await backend.UploadAsync(new MatchRecord { playerName = "slow", timeToBeatOpponent = 30f });
            await backend.UploadAsync(new MatchRecord { playerName = "fast", timeToBeatOpponent = 10f });

            IReadOnlyList<MatchRecord> top = await backend.FetchTopAsync(10);

            Assert.AreEqual(2, top.Count);
            Assert.AreEqual("fast", top[0].playerName);
            Assert.AreEqual("slow", top[1].playerName);
        }

        [Test]
        public async Task Memory_backend_fetch_top_limits_the_count()
        {
            var backend = new MemoryBackend();
            await backend.UploadAsync(new MatchRecord { timeToBeatOpponent = 1f });
            await backend.UploadAsync(new MatchRecord { timeToBeatOpponent = 2f });
            await backend.UploadAsync(new MatchRecord { timeToBeatOpponent = 3f });

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
                new MatchRecord { playerName = "p", damageDealt = 42, playerWon = true });

            StringAssert.Contains("\"playerName\":\"p\"", json);
            StringAssert.Contains("\"damageDealt\":42", json);
            StringAssert.Contains("\"playerWon\":true", json);
        }

        [Test]
        public void Rest_backend_parses_a_realtime_database_collection_response()
        {
            string response =
                "{\"-Na\":{\"playerName\":\"a\",\"timeToBeatOpponent\":20.0}," +
                "\"-Nb\":{\"playerName\":\"b\",\"timeToBeatOpponent\":5.0}}";

            List<MatchRecord> records = RestFirebaseBackend.ParseCollection(response);

            Assert.AreEqual(2, records.Count);
            MatchRecord a = records.Find(r => r.playerName == "a");
            MatchRecord b = records.Find(r => r.playerName == "b");
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreEqual(20.0f, a.timeToBeatOpponent, 0.0001f);
            Assert.AreEqual(5.0f, b.timeToBeatOpponent, 0.0001f);
        }

        [Test]
        public void Rest_backend_parses_an_empty_response_as_no_records()
        {
            Assert.AreEqual(0, RestFirebaseBackend.ParseCollection("null").Count);
            Assert.AreEqual(0, RestFirebaseBackend.ParseCollection("").Count);
        }
    }
}
