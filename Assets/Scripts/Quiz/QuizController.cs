using System;
using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Glue between QuizSession (rules) and QuizView (presentation). Feeds
    /// clicks and delta time into the session, shows feedback when the
    /// session resolves, and after a short delay raises QuestionAnswered so
    /// scoring/sequencing systems can react.
    /// </summary>
    public class QuizController : MonoBehaviour
    {
        [SerializeField] private QuizView view;

        [Tooltip("Feedback shown after a correct answer, before the next question is requested.")]
        [SerializeField, Min(0f)] private float feedbackDelaySeconds = 0.5f;

        [Tooltip("Feedback shown after a wrong answer or a timeout. Longer than the correct-answer delay on purpose: three of every four blind guesses are wrong, so this is what makes guessing cost real time.")]
        [SerializeField, Min(0f)] private float missFeedbackDelaySeconds = 1.2f;

        [Header("Answer lockout")]
        [Tooltip("How long a freshly shown question refuses answers. A click inside the window is ignored AND restarts it, so mashing one slot never resolves a question -- it times out instead. Set to 0 to accept answers immediately.")]
        [SerializeField, Min(0f)] private float answerLockoutSeconds = 0.35f;

        [Tooltip("Shuffles which slot each answer is shown in. Generated questions already randomize this; the shuffle extends it to authored ones so no fixed click position can be pre-committed.")]
        [SerializeField] private bool shuffleAnswerOrder = true;

        [Tooltip("Optional. Shown automatically on Start for quick play-mode testing.")]
        [SerializeField] private QuizQuestion startingQuestion;

        /// <summary>
        /// Fires the instant a question resolves (click or timeout), before
        /// the feedback delay. The float is the session's
        /// NormalizedTimeRemaining at resolution: 1 = answered instantly,
        /// 0 = timed out. Hook instant rewards (Flow meter, action points)
        /// here so they are not held back by the on-screen feedback.
        /// </summary>
        public event Action<QuizResult, float> QuestionResolved;

        /// <summary>
        /// Fires after the feedback colors have been shown for
        /// feedbackDelaySeconds. Same payload as QuestionResolved. Drives
        /// question sequencing (QuizFlow).
        /// </summary>
        public event Action<QuizResult, float> QuestionAnswered;

        private QuizSession session;
        private float feedbackTimer;
        private bool eventFired;
        private bool answersLocked;
        private System.Random shuffleRandom;

        private void OnEnable()
        {
            view.AnswerClicked += OnAnswerClicked;
        }

        private void OnDisable()
        {
            view.AnswerClicked -= OnAnswerClicked;
        }

        private void Start()
        {
            if (startingQuestion != null)
            {
                ShowQuestion(startingQuestion);
            }
        }

        /// <summary>Starts a fresh round with the given question.</summary>
        public void ShowQuestion(QuizQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("QuizController.ShowQuestion called with null question", this);
                return;
            }

            int[] order = BuildAnswerOrder();
            int correctSlot = AnswerOrder.SlotOf(order, question.correctIndex);
            if (correctSlot < 0)
            {
                // Only reachable from a question whose correctIndex is outside
                // the four answers; show it unshuffled rather than refusing it.
                Debug.LogError(
                    $"QuizController: '{question.questionText}' has correctIndex {question.correctIndex}, "
                    + "which is not one of the four answers. Showing the answers unshuffled.", this);
                order = AnswerOrder.Identity(QuizQuestion.AnswerCount);
                correctSlot = Mathf.Clamp(question.correctIndex, 0, QuizQuestion.AnswerCount - 1);
            }

            session = new QuizSession(correctSlot, question.timeLimitSeconds, answerLockoutSeconds);
            feedbackTimer = 0f;
            eventFired = false;
            view.ShowQuestion(question, order);

            // Everything downstream of here -- clicks, feedback colours, the
            // session's CorrectIndex -- works in slot space, so the permutation
            // never has to be undone.
            answersLocked = !session.IsAnswerable;
            view.SetAnswersLocked(answersLocked);
        }

        /// <summary>
        /// Builds the slot order for the next question: shuffled when the
        /// shuffle is on, otherwise the authored order.
        /// </summary>
        private int[] BuildAnswerOrder()
        {
            if (!shuffleAnswerOrder)
            {
                return AnswerOrder.Identity(QuizQuestion.AnswerCount);
            }

            if (shuffleRandom == null)
            {
                shuffleRandom = new System.Random();
            }
            return AnswerOrder.Shuffled(QuizQuestion.AnswerCount, shuffleRandom);
        }

        private void Update()
        {
            if (session == null || eventFired)
            {
                return;
            }

            if (!session.IsResolved)
            {
                session.Tick(Time.deltaTime);
                view.SetTimerFill(session.NormalizedTimeRemaining, session.RemainingTime);
                RefreshLockedVisual();
                if (session.IsResolved)
                {
                    ShowResolution();
                }
            }
            else
            {
                feedbackTimer += Time.deltaTime;
                if (feedbackTimer >= FeedbackDelayFor(session.Result))
                {
                    eventFired = true;
                    QuestionAnswered?.Invoke(session.Result, session.NormalizedTimeRemaining);
                }
            }
        }

        /// <summary>
        /// Repaints the answer buttons when the lockout opens or is pushed back
        /// by a click, so the dimming always matches whether a click would count.
        /// </summary>
        private void RefreshLockedVisual()
        {
            bool locked = !session.IsAnswerable;
            if (locked == answersLocked)
            {
                return;
            }

            answersLocked = locked;
            view.SetAnswersLocked(locked);
        }

        /// <summary>
        /// How long the feedback for the given result stays on screen. Misses
        /// linger longer than correct answers, which is what stops a guesser
        /// cycling questions as fast as a solver.
        /// </summary>
        private float FeedbackDelayFor(QuizResult result)
        {
            if (result == QuizResult.Correct)
            {
                return feedbackDelaySeconds;
            }
            return missFeedbackDelaySeconds;
        }

        private void OnAnswerClicked(int index)
        {
            if (session == null || session.IsResolved)
            {
                return;
            }

            session.SelectAnswer(index);
            if (session.IsResolved)
            {
                ShowResolution();
            }
        }

        private void ShowResolution()
        {
            view.ShowResult(session.Result, session.SelectedIndex, session.CorrectIndex);
            QuestionResolved?.Invoke(session.Result, session.NormalizedTimeRemaining);
        }
    }
}
