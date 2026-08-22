using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Endless quiz driver: cycles through the authored question list (wrapping
    /// after the last), optionally mixed with randomly generated math
    /// questions. Each round rolls mathChance to decide which kind is shown.
    /// </summary>
    public class QuizFlow : MonoBehaviour
    {
        [SerializeField] private QuizController quiz;
        [SerializeField] private QuizQuestion[] questions;

        [Header("Random math questions")]
        [SerializeField] private bool includeRandomMath;
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Chance per round that a random math question is shown instead of the next authored one.")]
        private float mathChance = 0.5f;
        [SerializeField, Min(1f)] private float mathTimeLimitSeconds = 5f;

        private MathQuestionGenerator generator;
        private int current;
        private QuizQuestion displayedGenerated;

        private void Awake()
        {
            generator = new MathQuestionGenerator { TimeLimitSeconds = mathTimeLimitSeconds };
        }

        private void OnEnable()
        {
            quiz.QuestionAnswered += OnAnswered;
        }

        private void OnDisable()
        {
            quiz.QuestionAnswered -= OnAnswered;
        }

        private void Start()
        {
            int valid = 0;
            for (int i = 0; i < (questions?.Length ?? 0); i++)
            {
                if (questions[i] != null)
                    questions[valid++] = questions[i];
                else
                    Debug.LogWarning($"QuizFlow: skipping missing question at index {i}", this);
            }
            if (valid != (questions?.Length ?? 0))
                System.Array.Resize(ref questions, valid);

            if (valid == 0 && !includeRandomMath)
            {
                Debug.LogError("QuizFlow has no questions assigned and random math is off", this);
                enabled = false;
                return;
            }

            ShowNext();
        }

        private void OnAnswered(QuizResult result)
        {
            ShowNext();
        }

        private void ShowNext()
        {
            bool noAuthored = questions == null || questions.Length == 0;
            bool useMath = includeRandomMath && (noAuthored || Random.value < mathChance);

            QuizQuestion next;
            if (useMath)
            {
                next = generator.Next();
            }
            else
            {
                next = questions[current];
                current = (current + 1) % questions.Length;
            }

            var previousGenerated = displayedGenerated;
            quiz.ShowQuestion(next);

            // Generated questions are runtime-only ScriptableObjects; destroy
            // the one no longer shown so endless play doesn't accumulate them.
            // Authored questions are assets and are never destroyed.
            if (previousGenerated != null)
                Destroy(previousGenerated);
            displayedGenerated = useMath ? next : null;
        }
    }
}
