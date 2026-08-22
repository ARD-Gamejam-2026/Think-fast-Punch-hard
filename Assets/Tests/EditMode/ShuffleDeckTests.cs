using System.Linq;
using NUnit.Framework;

namespace ThinkFast.Quiz.Tests
{
    public class ShuffleDeckTests
    {
        [Test]
        public void Next_CoversEveryIndexOncePerCycle()
        {
            var deck = new ShuffleDeck(10, new System.Random(1));

            for (int cycle = 0; cycle < 5; cycle++)
            {
                var drawn = Enumerable.Range(0, 10).Select(_ => deck.Next()).OrderBy(i => i);
                Assert.That(drawn, Is.EqualTo(Enumerable.Range(0, 10)));
            }
        }

        [Test]
        public void Next_NeverRepeatsAcrossCycleBoundary()
        {
            var deck = new ShuffleDeck(5, new System.Random(2));

            int previous = deck.Next();
            for (int i = 0; i < 500; i++)
            {
                int next = deck.Next();
                Assert.That(next, Is.Not.EqualTo(previous));
                previous = next;
            }
        }

        [Test]
        public void Next_SingleEntryDeckAlwaysReturnsZero()
        {
            var deck = new ShuffleDeck(1, new System.Random(3));

            Assert.That(deck.Next(), Is.Zero);
            Assert.That(deck.Next(), Is.Zero);
        }

        [Test]
        public void Constructor_NonPositiveCount_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new ShuffleDeck(0, new System.Random(4)));
        }
    }
}
