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
        /// The question time limit at the current difficulty: <paramref name="startSeconds"/>
        /// at difficulty 0, easing to <paramref name="endSeconds"/> at difficulty 1.
        /// The same absolute range for every question, regardless of its authored limit.
        /// </summary>
        public static float TimeLimitFor(float startSeconds, float endSeconds, float difficulty01)
        {
            return Mathf.Lerp(startSeconds, endSeconds, Mathf.Clamp01(difficulty01));
        }
    }
}
