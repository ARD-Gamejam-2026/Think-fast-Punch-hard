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

        /// <summary>
        /// Normalized time remaining above which the timer bar is still drawn in
        /// the fast zone colour. Exposed so a reward system can pay out on
        /// exactly the zone the player can see, rather than on a second copy of
        /// the same number.
        /// </summary>
        public float FastZoneNormalized => fastZoneNormalized;

        private void Awake()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].Initialize(i, Letters[i], index => AnswerClicked?.Invoke(index));
            }
        }

        /// <summary>
        /// Displays a question and resets all button/timer visuals. The order
        /// maps each button slot to the answer index it shows; pass null to show
        /// the answers in their authored order.
        /// </summary>
        public void ShowQuestion(QuizQuestion question, int[] order)
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

            for (int slot = 0; slot < answerButtons.Length; slot++)
            {
                ShowAnswerInSlot(question, slot, SourceIndexFor(order, slot));
            }
        }

        /// <summary>
        /// Resolves which answer index a button slot shows: the order's entry
        /// when there is a usable one, otherwise the slot's own index.
        /// </summary>
        private static int SourceIndexFor(int[] order, int slot)
        {
            if (order != null && slot < order.Length)
            {
                return order[slot];
            }
            return slot;
        }

        private void ShowAnswerInSlot(QuizQuestion question, int slot, int sourceIndex)
        {
            Sprite answerImage = null;
            if (question.answerImages != null && sourceIndex < question.answerImages.Length)
            {
                answerImage = question.answerImages[sourceIndex];
            }
            if (answerImage != null)
            {
                answerButtons[slot].SetAnswerImage(answerImage);
            }
            else
            {
                answerButtons[slot].SetAnswerText(question.answers[sourceIndex]);
            }
            answerButtons[slot].SetVisualState(AnswerButton.VisualState.Normal);
            answerButtons[slot].SetInteractable(true);
        }

        /// <summary>
        /// Dims the answer buttons while the opening lockout is running, and
        /// restores them when it ends.
        ///
        /// The buttons stay interactable on purpose: a disabled Unity Button
        /// swallows the click entirely, and the session needs to SEE a click
        /// during the lockout in order to push the window back. Dimming is the
        /// signal that a click will not count yet.
        /// </summary>
        public void SetAnswersLocked(bool locked)
        {
            foreach (var answerButton in answerButtons)
            {
                if (locked)
                {
                    answerButton.SetVisualState(AnswerButton.VisualState.Locked);
                }
                else
                {
                    answerButton.SetVisualState(AnswerButton.VisualState.Normal);
                }
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
