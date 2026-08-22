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

        [Test]
        public void Pick_OverUniformRolls_SelectsInProportionToWeight()
        {
            // A uniform sweep of the roll must land in each bucket in
            // proportion to its weight: {1, 1, 2} of total 4 -> 25/25/50 %.
            var weights = new[] { 1f, 1f, 2f };
            var counts = CountSelections(weights, 1000);

            Assert.That(counts[0], Is.EqualTo(250).Within(2));
            Assert.That(counts[1], Is.EqualTo(250).Within(2));
            Assert.That(counts[2], Is.EqualTo(500).Within(2));
        }

        [Test]
        public void Pick_DynamicBucketsWithGaps_SkipZeroAndStayProportional()
        {
            // Mirrors QuizFlow's dynamic buckets: authored 1, math disabled,
            // then three Wikipedia sources 2 / unavailable / 1. Zero-weight
            // buckets are never picked; the rest keep their proportions.
            var weights = new[] { 1f, 0f, 2f, 0f, 1f };
            var counts = CountSelections(weights, 1000);

            Assert.That(counts[1], Is.Zero);
            Assert.That(counts[3], Is.Zero);
            Assert.That(counts[0], Is.EqualTo(250).Within(2));
            Assert.That(counts[2], Is.EqualTo(500).Within(2));
            Assert.That(counts[4], Is.EqualTo(250).Within(2));
        }

        private static int[] CountSelections(float[] weights, int samples)
        {
            var counts = new int[weights.Length];
            for (int k = 0; k < samples; k++)
            {
                int selected = WeightedPicker.Pick(weights, k / (float)samples);
                counts[selected]++;
            }
            return counts;
        }
    }
}
