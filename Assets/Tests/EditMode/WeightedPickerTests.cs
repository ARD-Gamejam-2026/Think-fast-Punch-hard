using NUnit.Framework;

namespace ThinkFast.Quiz.Tests
{
    public class WeightedPickerTests
    {
        [Test]
        public void Pick_SinglePositiveBucket_AlwaysThatBucket()
        {
            var weights = new[] { 0f, 3f, 0f };

            Assert.That(WeightedPicker.Pick(weights, 0f), Is.EqualTo(1));
            Assert.That(WeightedPicker.Pick(weights, 0.5f), Is.EqualTo(1));
            Assert.That(WeightedPicker.Pick(weights, 1f), Is.EqualTo(1));
        }

        [Test]
        public void Pick_ZeroAndNegativeWeightBuckets_AreNeverPicked()
        {
            var weights = new[] { 0f, -2f, 5f };

            for (int i = 0; i <= 10; i++)
            {
                Assert.That(WeightedPicker.Pick(weights, i / 10f), Is.EqualTo(2));
            }
        }

        [Test]
        public void Pick_NoPositiveWeight_ReturnsMinusOne()
        {
            Assert.That(WeightedPicker.Pick(new[] { 0f, 0f, -1f }, 0.5f), Is.EqualTo(-1));
        }

        [Test]
        public void Pick_EmptyWeights_ReturnsMinusOne()
        {
            Assert.That(WeightedPicker.Pick(new float[0], 0.5f), Is.EqualTo(-1));
        }

        [Test]
        public void Pick_BoundaryRollOfOne_StaysOnLastPositiveBucket()
        {
            // Trailing zero bucket must not be reached by a roll of exactly 1.
            Assert.That(WeightedPicker.Pick(new[] { 2f, 0f }, 1f), Is.EqualTo(0));
            Assert.That(WeightedPicker.Pick(new[] { 1f, 1f, 2f }, 1f), Is.EqualTo(2));
        }

        [Test]
        public void Pick_RollSelectsBucketProportionally()
        {
            // Weights {1, 1, 2}, total 4: roll < 1 -> 0, [1, 2) -> 1, [2, 4) -> 2.
            var weights = new[] { 1f, 1f, 2f };

            Assert.That(WeightedPicker.Pick(weights, 0f), Is.EqualTo(0));
            Assert.That(WeightedPicker.Pick(weights, 0.2f), Is.EqualTo(0));
            Assert.That(WeightedPicker.Pick(weights, 0.25f), Is.EqualTo(1));
            Assert.That(WeightedPicker.Pick(weights, 0.4f), Is.EqualTo(1));
            Assert.That(WeightedPicker.Pick(weights, 0.5f), Is.EqualTo(2));
            Assert.That(WeightedPicker.Pick(weights, 0.9f), Is.EqualTo(2));
        }
    }
}
