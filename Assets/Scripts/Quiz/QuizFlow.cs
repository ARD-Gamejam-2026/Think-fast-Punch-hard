using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Endless quiz driver. Each round rolls among the question types that
    /// are currently available — authored list (cycling), generated math,
    /// prefetched place questions — proportionally to their weights.
    /// </summary>
    public class QuizFlow : MonoBehaviour
    {
        [SerializeField] private QuizController quiz;
        [SerializeField] private QuizQuestion[] questions;

        [Header("Type mixing (relative weights per round)")]
        [SerializeField, Min(0f)] private float authoredWeight = 1f;
        [SerializeField, Min(0f)] private float mathWeight = 1f;
        [SerializeField, Min(0f)] private float placeWeight;

        [Header("Random math questions")]
        [SerializeField, Min(1f)] private float mathTimeLimitSeconds = 5f;

        [Header("Place questions")]
        [SerializeField] private PlaceQuestionSource placeSource;

        private const float RetrySeconds = 0.5f;

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
            CancelInvoke(nameof(ShowNext));
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

            bool anyConfigured =
                (valid > 0 && authoredWeight > 0f)
                || mathWeight > 0f
                || (placeSource != null && placeWeight > 0f);
            if (!anyConfigured)
            {
                Debug.LogError("QuizFlow has no available question type", this);
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
            float authored = questions.Length > 0 ? authoredWeight : 0f;
            float math = mathWeight;
            float place = placeSource != null && placeSource.HasQuestion ? placeWeight : 0f;
            float total = authored + math + place;

            if (total <= 0f)
            {
                // Nothing available right now (e.g. places-only while the
                // queue still fills). Retry shortly instead of stalling.
                Invoke(nameof(ShowNext), RetrySeconds);
                return;
            }

            float roll = Random.value * total;
            QuizQuestion next;
            bool generated;
            if (roll < authored)
            {
                next = questions[current];
                current = (current + 1) % questions.Length;
                generated = false;
            }
            else if (roll < authored + math)
            {
                next = generator.Next();
                generated = true;
            }
            else
            {
                next = placeSource.Dequeue();
                generated = true;
            }

            var previousGenerated = displayedGenerated;
            quiz.ShowQuestion(next);

            // Generated questions (and their downloaded sprites/textures) are
            // runtime-only; destroy the one no longer shown so endless play
            // doesn't accumulate them. Authored assets are never destroyed.
            if (previousGenerated != null)
            {
                if (previousGenerated.image != null)
                {
                    var texture = previousGenerated.image.texture;
                    Destroy(previousGenerated.image);
                    Destroy(texture);
                }
                Destroy(previousGenerated);
            }
            displayedGenerated = generated ? next : null;
        }
    }
}
