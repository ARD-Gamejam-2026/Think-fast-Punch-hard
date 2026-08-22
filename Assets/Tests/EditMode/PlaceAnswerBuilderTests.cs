using System.Linq;
using NUnit.Framework;

namespace ThinkFast.Quiz.Tests
{
    public class PlaceAnswerBuilderTests
    {
        private static LandmarkList.Entry[] MakeEntries(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new LandmarkList.Entry
                {
                    wikipediaTitle = $"Place_{i}",
                    displayName = $"Place {i}",
                })
                .ToArray();
        }

        [Test]
        public void Build_ReturnsFourUniqueAnswersWithCorrectOne()
        {
            var entries = MakeEntries(10);
            var random = new System.Random(1);

            for (int i = 0; i < 100; i++)
            {
                int correctEntry = i % entries.Length;
                var answers = PlaceAnswerBuilder.Build(entries, correctEntry, random, out int correctIndex);

                Assert.That(answers.Length, Is.EqualTo(QuizQuestion.AnswerCount));
                Assert.That(answers, Is.Unique);
                Assert.That(answers[correctIndex], Is.EqualTo(entries[correctEntry].displayName));
            }
        }

        [Test]
        public void Build_DistractorsComeFromOtherEntries()
        {
            var entries = MakeEntries(4);
            var answers = PlaceAnswerBuilder.Build(entries, 0, new System.Random(2), out _);

            Assert.That(answers.OrderBy(a => a),
                Is.EqualTo(entries.Select(e => e.displayName).OrderBy(a => a)));
        }

        [Test]
        public void Build_FewerThanFourEntries_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => PlaceAnswerBuilder.Build(MakeEntries(3), 0, new System.Random(3), out _));
        }
    }
}
