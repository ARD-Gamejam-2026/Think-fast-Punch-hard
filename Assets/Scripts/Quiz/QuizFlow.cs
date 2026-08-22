using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Endless quiz driver. Each round rolls among the question types that
    /// are currently available — authored list (cycling), generated math,
    /// prefetched Wikipedia topics (places, animals, ...) — proportionally
    /// to their weights.
    /// </summary>
    public class QuizFlow : MonoBehaviour
    {
        /// <summary>One prefetching Wikipedia source and its relative roll weight in the mix.</summary>
        [System.Serializable]
        public class WeightedWikipediaSource
        {
            /// <summary>Source component that prefetches the questions for one topic list.</summary>
            public WikipediaQuestionSource source;

            /// <summary>Relative weight of this source per round, alongside the other types.</summary>
            [Min(0f)] public float weight = 1f;
        }

        [SerializeField] private QuizController quiz;
        [SerializeField] private QuizQuestion[] questions;

        [Header("Type mixing (relative weights per round)")]
        [SerializeField, Min(0f)] private float authoredWeight = 1f;
        [SerializeField, Min(0f)] private float mathWeight = 1f;

        [Header("Random math questions")]
        [SerializeField, Min(1f)] private float mathTimeLimitSeconds = 5f;

        [Header("Sequence questions (numbers & shapes)")]
        [SerializeField, Min(0f)] private float sequenceWeight = 1f;
        [SerializeField, Min(1f)] private float sequenceTimeLimitSeconds = 8f;
        [SerializeField, Range(0f, 1f)] private float sequenceShapeShare = 0.5f;

        [Header("Wikipedia questions (places, animals, ...)")]
        [SerializeField] private WeightedWikipediaSource[] wikipediaSources = new WeightedWikipediaSource[0];

        private const float RetrySeconds = 0.5f;

        // Weight indices 0, 1, and 2 are the fixed buckets; Wikipedia sources
        // follow at FixedBuckets + i.
        private const int AuthoredBucket = 0;
        private const int MathBucket = 1;
        private const int SequenceBucket = 2;
        private const int FixedBuckets = 3;

        private MathQuestionGenerator generator;
        private SequenceQuestionGenerator sequenceGenerator;
        private int current;
        private QuizQuestion displayedGenerated;

        private void Awake()
        {
            generator = new MathQuestionGenerator { TimeLimitSeconds = mathTimeLimitSeconds };
            sequenceGenerator = new SequenceQuestionGenerator
            {
                TimeLimitSeconds = sequenceTimeLimitSeconds,
                ShapeShare = sequenceShapeShare,
            };
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
                {
                    questions[valid++] = questions[i];
                }
                else
                {
                    Debug.LogWarning($"QuizFlow: skipping missing question at index {i}", this);
                }
            }
            if (valid != (questions?.Length ?? 0))
            {
                System.Array.Resize(ref questions, valid);
            }

            if (!AnyTypeConfigured(valid))
            {
                Debug.LogError("QuizFlow has no available question type", this);
                enabled = false;
                return;
            }

            ShowNext();
        }

        private bool AnyTypeConfigured(int authoredCount)
        {
            if (authoredCount > 0 && authoredWeight > 0f)
            {
                return true;
            }
            if (mathWeight > 0f)
            {
                return true;
            }
            if (sequenceWeight > 0f)
            {
                return true;
            }
            foreach (var weighted in wikipediaSources)
            {
                if (weighted != null && weighted.source != null && weighted.weight > 0f)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnAnswered(QuizResult result, float normalizedTimeRemaining)
        {
            ShowNext();
        }

        private void ShowNext()
        {
            float[] weights = BuildAvailableWeights();
            int selected = WeightedPicker.Pick(weights, Random.value);

            if (selected < 0)
            {
                // Nothing available right now (e.g. Wikipedia-only while the
                // queues still fill). Retry shortly instead of stalling.
                Invoke(nameof(ShowNext), RetrySeconds);
                return;
            }

            QuizQuestion next = TakeQuestion(selected);

            var previousGenerated = displayedGenerated;
            quiz.ShowQuestion(next);

            // Generated questions (and their downloaded sprites/textures) are
            // runtime-only; destroy the one no longer shown so endless play
            // doesn't accumulate them. Authored assets are never destroyed.
            DestroyGeneratedVisuals(previousGenerated);

            if (selected == AuthoredBucket)
            {
                displayedGenerated = null;
            }
            else
            {
                displayedGenerated = next;
            }
        }

        /// <summary>
        /// Builds the per-bucket weights for this round; unavailable buckets
        /// (empty authored list, Wikipedia source with no prefetched question)
        /// get weight 0.
        /// </summary>
        private float[] BuildAvailableWeights()
        {
            var weights = new float[FixedBuckets + wikipediaSources.Length];
            if (questions.Length > 0)
            {
                weights[AuthoredBucket] = authoredWeight;
            }
            weights[MathBucket] = mathWeight;
            weights[SequenceBucket] = sequenceWeight;
            for (int i = 0; i < wikipediaSources.Length; i++)
            {
                var weighted = wikipediaSources[i];
                if (weighted != null && weighted.source != null && weighted.source.HasQuestion)
                {
                    weights[FixedBuckets + i] = weighted.weight;
                }
            }
            return weights;
        }

        private QuizQuestion TakeQuestion(int selected)
        {
            switch (selected)
            {
                case AuthoredBucket:
                    QuizQuestion next = questions[current];
                    current = (current + 1) % questions.Length;
                    return next;
                case MathBucket:
                    return generator.Next();
                case SequenceBucket:
                    return sequenceGenerator.Next();
                default:
                    // A Wikipedia bucket only gets weight when its source has
                    // a prefetched question; the guard makes that invariant
                    // explicit (falls back to math otherwise).
                    var weighted = wikipediaSources[selected - FixedBuckets];
                    if (weighted != null && weighted.source != null && weighted.source.HasQuestion)
                    {
                        return weighted.source.Dequeue();
                    }
                    return generator.Next();
            }
        }

        private void DestroyGeneratedVisuals(QuizQuestion generated)
        {
            if (generated == null)
            {
                return;
            }
            DestroySprite(generated.image);
            if (generated.answerImages != null)
            {
                foreach (var answerImage in generated.answerImages)
                {
                    DestroySprite(answerImage);
                }
            }
            Destroy(generated);
        }

        private void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }
            Texture texture = sprite.texture;
            Destroy(sprite);
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
