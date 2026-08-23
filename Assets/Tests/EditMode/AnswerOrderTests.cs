using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ThinkFast.Quiz.Tests
{
    public class AnswerOrderTests
    {
        [Test]
        public void Identity_MapsEverySlotToItsOwnIndex()
        {
            int[] order = AnswerOrder.Identity(4);

            Assert.That(order, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void Shuffled_IsAPermutationOfEveryIndex()
        {
            var random = new Random(1234);

            for (int round = 0; round < 50; round++)
            {
                int[] order = AnswerOrder.Shuffled(4, random);

                Assert.That(order.Length, Is.EqualTo(4));
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, order);
            }
        }

        [Test]
        public void Shuffled_MovesTheCorrectAnswerAround()
        {
            var random = new Random(7);
            var slotsSeen = new HashSet<int>();

            for (int round = 0; round < 100; round++)
            {
                int[] order = AnswerOrder.Shuffled(4, random);
                slotsSeen.Add(AnswerOrder.SlotOf(order, 3));
            }

            // A fixed authored correctIndex has to be able to land anywhere,
            // otherwise a click position could still be pre-committed.
            Assert.That(slotsSeen, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void SlotOf_FindsTheSlotShowingTheAnswer()
        {
            int[] order = { 2, 0, 3, 1 };

            Assert.That(AnswerOrder.SlotOf(order, 2), Is.EqualTo(0));
            Assert.That(AnswerOrder.SlotOf(order, 0), Is.EqualTo(1));
            Assert.That(AnswerOrder.SlotOf(order, 3), Is.EqualTo(2));
            Assert.That(AnswerOrder.SlotOf(order, 1), Is.EqualTo(3));
        }

        [Test]
        public void SlotOf_UnknownAnswer_ReturnsMinusOne()
        {
            int[] order = { 2, 0, 3, 1 };

            Assert.That(AnswerOrder.SlotOf(order, 4), Is.EqualTo(-1));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AnswerOrder.Identity(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => AnswerOrder.Shuffled(0, new Random(1)));
            Assert.Throws<ArgumentNullException>(() => AnswerOrder.Shuffled(4, null));
            Assert.Throws<ArgumentNullException>(() => AnswerOrder.SlotOf(null, 0));
        }
    }
}
