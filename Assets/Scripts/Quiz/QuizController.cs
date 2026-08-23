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

        [Tooltip("Feedback shown before the next question is requested, after a correct answer or a timeout. Lets the player see the result before the next question loads.")]
        [SerializeField, Min(0f)] private float feedbackDelaySeconds = 0.5f;

        [Tooltip("Feedback shown before the next question is requested, after a WRONG answer. Longer than the correct/timeout delay on purpose: it is the penalty for guessing wrong -- the next question takes longer to arrive.")]
        [SerializeField, Min(0f)] private float missFeedbackDelaySeconds = 1.5f;

        [Tooltip("Shuffles which slot each answer is shown in. Generated questions already randomize this; the shuffle extends it to authored ones so no fixed click position can be pre-committed.")]
        [SerializeField] private bool shuffleAnswerOrder = true;

        [Tooltip("Optional. Shown automatically on Start for quick play-mode testing.")]
        [SerializeField] private QuizQuestion startingQuestion;

        [Header("Difficulty")]
        [Tooltip("Timer multiplier at maximum difficulty. 0.5 = late-game questions get half their authored time. 1 disables the shrink.")]
        [SerializeField, Range(0f, 1f)] private float timerScaleAtMaxDifficulty = 0.5f;

        [Tooltip("Lower bound on the scaled time limit, in seconds, so late-game questions stay answerable. Never lengthens a shorter authored limit.")]
        [SerializeField, Min(0.5f)] private float minTimerSeconds = 3f;

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

            // Everything downstream of here -- clicks, feedback colours, the
            // session's CorrectIndex -- works in slot space, so the permutation
            // never has to be undone.
            float timeLimit = DifficultyMath.ScaleTimeLimit(
                question.timeLimitSeconds,
                DifficultyRamp.Current01,
                timerScaleAtMaxDifficulty,
                minTimerSeconds);
            session = new QuizSession(correctSlot, timeLimit);
            feedbackTimer = 0f;
            eventFired = false;
            view.ShowQuestion(question, order);
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
        /// How long the resolved question stays on screen before the next one
        /// loads. Only a wrong answer lingers longer; a correct answer and a
        /// timeout both use the short delay, which is what makes a wrong guess
        /// cost real time while an honest solver keeps pace.
        /// </summary>
        private float FeedbackDelayFor(QuizResult result)
        {
            if (result == QuizResult.Wrong)
            {
                return missFeedbackDelaySeconds;
            }
            return feedbackDelaySeconds;
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
