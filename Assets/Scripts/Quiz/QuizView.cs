using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Presentation of the quiz panel: question text, optional image, timer
    /// bar, four answer buttons. Contains no game rules.
    /// </summary>
    public class QuizView : MonoBehaviour
    {
        private static readonly string[] Letters = { "A", "B", "C", "D" };

        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private GameObject imagePanel;
        [SerializeField] private Image questionImage;
        [SerializeField] private Image timerFill;
        [SerializeField] private AnswerButton[] answerButtons = new AnswerButton[QuizQuestion.AnswerCount];

        public event Action<int> AnswerClicked;

        private void Awake()
        {
            for (int i = 0; i < answerButtons.Length; i++)
                answerButtons[i].Initialize(i, Letters[i], index => AnswerClicked?.Invoke(index));
        }

        public void ShowQuestion(QuizQuestion question)
        {
            questionLabel.text = question.questionText;
            imagePanel.SetActive(question.image != null);
            questionImage.sprite = question.image;
            SetTimerFill(1f);

            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].SetAnswerText(question.answers[i]);
                answerButtons[i].SetVisualState(AnswerButton.VisualState.Normal);
                answerButtons[i].SetInteractable(true);
            }
        }

        public void SetTimerFill(float normalized)
        {
            timerFill.fillAmount = Mathf.Clamp01(normalized);
        }

        public void ShowResult(QuizResult result, int selectedIndex, int correctIndex)
        {
            foreach (var answerButton in answerButtons)
                answerButton.SetInteractable(false);

            switch (result)
            {
                case QuizResult.Correct:
                    answerButtons[correctIndex].SetVisualState(AnswerButton.VisualState.Correct);
                    break;
                case QuizResult.Wrong:
                    answerButtons[selectedIndex].SetVisualState(AnswerButton.VisualState.Wrong);
                    answerButtons[correctIndex].SetVisualState(AnswerButton.VisualState.Highlighted);
                    break;
                case QuizResult.TimedOut:
                    answerButtons[correctIndex].SetVisualState(AnswerButton.VisualState.Highlighted);
                    SetTimerFill(0f);
                    break;
            }
        }
    }
}
