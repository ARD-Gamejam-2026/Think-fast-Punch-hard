using NUnit.Framework;
using UnityEngine;

namespace ThinkFast.Quiz.Tests
{
    public class WikipediaSummaryTests
    {
        [Test]
        public void FromJson_ParsesTitleAndThumbnail()
        {
            const string json = @"{
                ""type"": ""standard"",
                ""title"": ""Brandenburg Gate"",
                ""thumbnail"": {
                    ""source"": ""https://upload.wikimedia.org/wikipedia/commons/thumb/a/a6/Brandenburger_Tor_abends.jpg/330px-Brandenburger_Tor_abends.jpg"",
                    ""width"": 330,
                    ""height"": 220
                },
                ""extract"": ""The Brandenburg Gate is ...""
            }";

            var summary = JsonUtility.FromJson<WikipediaSummary>(json);

            Assert.That(summary.title, Is.EqualTo("Brandenburg Gate"));
            Assert.That(summary.thumbnail.source, Does.StartWith("https://upload.wikimedia.org/"));
            Assert.That(summary.thumbnail.width, Is.EqualTo(330));
        }

        [Test]
        public void FromJson_MissingThumbnail_LeavesSourceEmpty()
        {
            var summary = JsonUtility.FromJson<WikipediaSummary>(@"{""title"": ""Somewhere""}");

            Assert.That(string.IsNullOrEmpty(summary.thumbnail?.source), Is.True);
        }
    }
}
