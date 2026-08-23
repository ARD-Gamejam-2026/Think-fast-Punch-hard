using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// The single source of match difficulty, as a normalized 0->1 value driven
    /// by the number of quiz questions answered correctly.
    ///
    /// It lives in the Quiz assembly on purpose: the signal that drives it --
    /// <see cref="QuizController.QuestionResolved"/> -- originates here, so the
    /// quiz reads difficulty locally and the fighter reads it across the one
    /// reference direction Unity allows (Assembly-CSharp -> Quiz). The value is a
    /// static, matching the RoundEvents/RiddleRewards seams, so a consumer needs
    /// no scene reference to it.
    ///
    /// This class is only a source. It does not know about timers or run speeds;
    /// each consumer maps <see cref="Current01"/> onto its own range.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DifficultyRamp : MonoBehaviour
    {
        [Tooltip("Left empty, the ramp looks for a QuizController on this object and then anywhere in the scene.")]
        [SerializeField] private QuizController quiz;

        [Tooltip("Correct answers needed to reach maximum difficulty (Current01 == 1).")]
        [SerializeField, Min(1)] private int correctAnswersToMax = 10;

        [Tooltip("Shapes progress (fraction of the way to correctAnswersToMax) into difficulty. Linear by default.")]
        [SerializeField] private AnimationCurve rampCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private static int correctCount;

        /// <summary>Normalized match difficulty, 0 at round start rising to 1. Defaults to 0.</summary>
        public static float Current01 { get; private set; }

        /// <summary>
        /// Zeroes the ramp. Called from Awake (a fresh scene is a fresh round) and
        /// exposed for the match layer to call on a future in-place restart.
        /// </summary>
        public static void ResetRamp()
        {
            correctCount = 0;
            Current01 = 0f;
        }

        // Static state survives entering Play mode with domain reloading off, which
        // would otherwise start a round already part-way up the ramp. Mirrors RoundEvents.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            ResetRamp();
        }

        private void Awake()
        {
            ResetRamp();

            if (quiz == null)
            {
                quiz = GetComponent<QuizController>();
            }
            if (quiz == null)
            {
                quiz = FindAnyObjectByType<QuizController>();
            }
        }

        private void OnEnable()
        {
            if (quiz == null)
            {
                Debug.LogError("DifficultyRamp has no QuizController, so difficulty will never rise.", this);
                return;
            }
            quiz.QuestionResolved += HandleResolved;
        }

        private void OnDisable()
        {
            if (quiz != null)
            {
                quiz.QuestionResolved -= HandleResolved;
            }
        }

        private void HandleResolved(QuizResult result, float speed)
        {
            if (result != QuizResult.Correct)
            {
                return;
            }
            correctCount++;
            Current01 = DifficultyMath.DifficultyFor(correctCount, correctAnswersToMax, rampCurve);
        }
    }
}
