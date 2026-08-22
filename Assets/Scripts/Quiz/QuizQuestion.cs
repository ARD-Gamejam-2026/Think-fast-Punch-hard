using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Content of one quiz question: text, optional image, four answers, the
    /// correct answer's index, and the countdown length.
    /// </summary>
    [CreateAssetMenu(menuName = "Quiz/Question", fileName = "QuizQuestion")]
    public class QuizQuestion : ScriptableObject
    {
        public const int AnswerCount = 4;

        [TextArea]
        public string questionText;

        [Tooltip("Optional. When null, the quiz panel hides the image area.")]
        public Sprite image;

        public string[] answers = new string[AnswerCount];

        [Tooltip("Optional per-answer images. When null, answers are text; when set, "
            + "each non-null entry replaces its answer button's text with the sprite.")]
        public Sprite[] answerImages;

        [Range(0, AnswerCount - 1)]
        public int correctIndex;

        [Min(1f)]
        public float timeLimitSeconds = 10f;

        /// <summary>Validates the data: repairs out-of-range values and warns about empty answers.</summary>
        public void OnValidate()
        {
            if (answers == null || answers.Length != AnswerCount)
            {
                System.Array.Resize(ref answers, AnswerCount);
            }

            if (answerImages != null && answerImages.Length != AnswerCount)
            {
                System.Array.Resize(ref answerImages, AnswerCount);
            }

            correctIndex = Mathf.Clamp(correctIndex, 0, AnswerCount - 1);

            if (timeLimitSeconds <= 0f)
            {
                timeLimitSeconds = 1f;
            }

            for (int i = 0; i < AnswerCount; i++)
            {
                if (string.IsNullOrWhiteSpace(answers[i]))
                {
                    Debug.LogWarning($"{name}: answer {i} is empty", this);
                }
            }
        }
    }
}
