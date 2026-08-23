using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Pure difficulty arithmetic, split out from <see cref="DifficultyRamp"/> and
    /// <see cref="QuizController"/> so it can be unit-tested without a scene.
    /// </summary>
    public static class DifficultyMath
    {
        /// <summary>
        /// Fraction of the way to maximum difficulty, 0..1, from the number of
        /// questions answered correctly. Returns 0 for a non-positive target.
        /// </summary>
        public static float Progress01(int correctCount, int correctAnswersToMax)
        {
            if (correctAnswersToMax < 1)
            {
                return 0f;
            }
            return Mathf.Clamp01((float)correctCount / correctAnswersToMax);
        }

        /// <summary>
        /// Difficulty 0..1: correct-answer progress shaped through the ramp curve.
        /// </summary>
        public static float DifficultyFor(int correctCount, int correctAnswersToMax, AnimationCurve curve)
        {
            float progress = Progress01(correctCount, correctAnswersToMax);
            return Mathf.Clamp01(curve.Evaluate(progress));
        }

        /// <summary>
        /// Scales an authored time limit by the current difficulty: full time at
        /// difficulty 0, <paramref name="scaleAtMaxDifficulty"/> of it at difficulty
        /// 1, never below <paramref name="minSeconds"/>.
        /// </summary>
        public static float ScaleTimeLimit(float authoredSeconds, float difficulty01, float scaleAtMaxDifficulty, float minSeconds)
        {
            float scale = Mathf.Lerp(1f, scaleAtMaxDifficulty, Mathf.Clamp01(difficulty01));
            return Mathf.Max(minSeconds, authoredSeconds * scale);
        }
    }
}
