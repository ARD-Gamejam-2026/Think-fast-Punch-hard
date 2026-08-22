using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Cycles through a list of questions endlessly: when one resolves
    /// (after the controller's feedback delay), the next is shown; after
    /// the last it wraps back to the first.
    /// </summary>
    public class QuizFlow : MonoBehaviour
    {
        [SerializeField] private QuizController quiz;
        [SerializeField] private QuizQuestion[] questions;

        private int current;

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
            if (questions == null || questions.Length == 0)
            {
                Debug.LogError("QuizFlow has no questions assigned", this);
                enabled = false;
                return;
            }

            quiz.ShowQuestion(questions[current]);
        }

        private void OnAnswered(QuizResult result)
        {
            current = (current + 1) % questions.Length;
            quiz.ShowQuestion(questions[current]);
        }
    }
}
