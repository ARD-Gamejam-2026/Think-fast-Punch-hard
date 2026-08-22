using UnityEngine;

namespace ThinkFast.Quiz
{
    [CreateAssetMenu(menuName = "Quiz/Question", fileName = "QuizQuestion")]
    public class QuizQuestion : ScriptableObject
    {
        public const int AnswerCount = 4;

        [TextArea]
        public string questionText;

        [Tooltip("Optional. When null, the quiz panel hides the image area.")]
        public Sprite image;

        public string[] answers = new string[AnswerCount];

        [Range(0, AnswerCount - 1)]
        public int correctIndex;

        [Min(1f)]
        public float timeLimitSeconds = 10f;

        public void OnValidate()
        {
            if (answers == null || answers.Length != AnswerCount)
                System.Array.Resize(ref answers, AnswerCount);

            correctIndex = Mathf.Clamp(correctIndex, 0, AnswerCount - 1);

            if (timeLimitSeconds <= 0f)
                timeLimitSeconds = 1f;

            for (int i = 0; i < AnswerCount; i++)
            {
                if (string.IsNullOrWhiteSpace(answers[i]))
                    Debug.LogWarning($"{name}: answer {i} is empty", this);
            }
        }
    }
}
