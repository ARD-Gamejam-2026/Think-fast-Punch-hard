using NUnit.Framework;
using UnityEngine;

namespace ThinkFast.Quiz.Tests
{
    public class DifficultyMathTests
    {
        private static AnimationCurve Linear => AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Test]
        public void Progress01_NoCorrect_IsZero()
        {
            Assert.That(DifficultyMath.Progress01(0, 10), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Progress01_Halfway_IsHalf()
        {
            Assert.That(DifficultyMath.Progress01(5, 10), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Progress01_AtTarget_IsOne()
        {
            Assert.That(DifficultyMath.Progress01(10, 10), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Progress01_BeyondTarget_ClampsToOne()
        {
            Assert.That(DifficultyMath.Progress01(15, 10), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Progress01_NonPositiveTarget_IsZero()
        {
            Assert.That(DifficultyMath.Progress01(3, 0), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void DifficultyFor_LinearCurve_MatchesProgress()
        {
            Assert.That(DifficultyMath.DifficultyFor(5, 10, Linear), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void TimeLimitFor_ZeroDifficulty_ReturnsStart()
        {
            Assert.That(DifficultyMath.TimeLimitFor(10f, 5f, 0f), Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void TimeLimitFor_MaxDifficulty_ReturnsEnd()
        {
            Assert.That(DifficultyMath.TimeLimitFor(10f, 5f, 1f), Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void TimeLimitFor_Halfway_InterpolatesBetween()
        {
            Assert.That(DifficultyMath.TimeLimitFor(10f, 5f, 0.5f), Is.EqualTo(7.5f).Within(1e-4f));
        }

        [Test]
        public void TimeLimitFor_DifficultyBeyondRange_ClampsToEnds()
        {
            Assert.That(DifficultyMath.TimeLimitFor(10f, 5f, 2f), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(DifficultyMath.TimeLimitFor(10f, 5f, -1f), Is.EqualTo(10f).Within(1e-4f));
        }
    }
}
