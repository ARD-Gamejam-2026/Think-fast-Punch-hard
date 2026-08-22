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
        [SerializeField] private Image questionImage;
        [SerializeField] private Image timerFill;
        [SerializeField] private AnswerButton[] answerButtons = new AnswerButton[QuizQuestion.AnswerCount];

        [Header("Timer colors (answer-speed zones)")]
        [Tooltip("Answering in this zone is a fast solve (builds Flow).")]
        [SerializeField] private Color timerFastColor = new Color(0.20f, 0.75f, 0.25f);
        [SerializeField] private Color timerMidColor = new Color(0.95f, 0.75f, 0.10f);
        [SerializeField] private Color timerLastSecondColor = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField, Range(0f, 1f)] private float fastZoneNormalized = 0.6f;
        [SerializeField, Min(0f)] private float lastSecondSeconds = 1f;

        public event Action<int> AnswerClicked;

        private void Awake()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].Initialize(i, Letters[i], index => AnswerClicked?.Invoke(index));
            }
        }

        /// <summary>Displays a question and resets all button/timer visuals.</summary>
        public void ShowQuestion(QuizQuestion question)
        {
            bool hasImage = question.image != null;
            questionLabel.text = question.questionText;
            // The top pane keeps its height either way; text-only questions
            // center their text in it, image questions push the text up.
            questionLabel.alignment = hasImage
                ? TextAlignmentOptions.Top
                : TextAlignmentOptions.Center;
            questionImage.enabled = hasImage;
            questionImage.sprite = question.image;
            SetTimerFill(1f, question.timeLimitSeconds);

            for (int i = 0; i < answerButtons.Length; i++)
            {
                Sprite answerImage = null;
                if (question.answerImages != null && i < question.answerImages.Length)
                {
                    answerImage = question.answerImages[i];
                }
                if (answerImage != null)
                {
                    answerButtons[i].SetAnswerImage(answerImage);
                }
                else
                {
                    answerButtons[i].SetAnswerText(question.answers[i]);
                }
                answerButtons[i].SetVisualState(AnswerButton.VisualState.Normal);
                answerButtons[i].SetInteractable(true);
            }
        }

        /// <summary>
        /// Updates the timer bar: fill amount plus the answer-speed zone color
        /// (fast/mid/last-second).
        /// </summary>
        public void SetTimerFill(float normalized, float remainingSeconds)
        {
            timerFill.fillAmount = Mathf.Clamp01(normalized);

            if (normalized > fastZoneNormalized)
            {
                timerFill.color = timerFastColor;
            }
            else if (remainingSeconds <= lastSecondSeconds)
            {
                timerFill.color = timerLastSecondColor;
            }
            else
            {
                timerFill.color = timerMidColor;
            }
        }

        /// <summary>
        /// Shows the end-of-question feedback: green on a correct pick, red on
        /// a wrong pick with the right answer in gold, gold-only on timeout.
        /// </summary>
        public void ShowResult(QuizResult result, int selectedIndex, int correctIndex)
        {
            foreach (var answerButton in answerButtons)
            {
                answerButton.SetInteractable(false);
            }

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
                    SetTimerFill(0f, 0f);
                    break;
                default:
                    break;
            }
        }
    }
}
