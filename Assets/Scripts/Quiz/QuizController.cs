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
        [SerializeField, Min(0f)] private float feedbackDelaySeconds = 0.5f;

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
                ShowQuestion(startingQuestion);
        }

        public void ShowQuestion(QuizQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("QuizController.ShowQuestion called with null question", this);
                return;
            }

            session = new QuizSession(question.correctIndex, question.timeLimitSeconds);
            feedbackTimer = 0f;
            eventFired = false;
            view.ShowQuestion(question);
        }

        private void Update()
        {
            if (session == null || eventFired)
                return;

            if (!session.IsResolved)
            {
                session.Tick(Time.deltaTime);
                view.SetTimerFill(session.NormalizedTimeRemaining, session.RemainingTime);
                if (session.IsResolved)
                    ShowResolution();
            }
            else
            {
                feedbackTimer += Time.deltaTime;
                if (feedbackTimer >= feedbackDelaySeconds)
                {
                    eventFired = true;
                    QuestionAnswered?.Invoke(session.Result, session.NormalizedTimeRemaining);
                }
            }
        }

        private void OnAnswerClicked(int index)
        {
            if (session == null || session.IsResolved)
                return;

            session.SelectAnswer(index);
            if (session.IsResolved)
                ShowResolution();
        }

        private void ShowResolution()
        {
            view.ShowResult(session.Result, session.SelectedIndex, session.CorrectIndex);
            QuestionResolved?.Invoke(session.Result, session.NormalizedTimeRemaining);
        }
    }
}
