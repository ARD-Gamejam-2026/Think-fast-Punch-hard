using System;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Rules of a single question in progress. Plain C#, no scene dependencies,
    /// so it is fully covered by EditMode tests. Resolves exactly once to
    /// Correct, Wrong, or TimedOut; all input is ignored afterwards.
    ///
    /// The opening answer lockout is what stops a player farming rewards by
    /// mashing one answer slot: a click inside the window is not just ignored,
    /// it re-arms the window. Sustained mashing therefore never resolves the
    /// question at all -- the countdown runs out underneath it and the question
    /// times out, paying nothing. One deliberate click after the window costs
    /// the player nothing.
    /// </summary>
    public class QuizSession
    {
        public const int AnswerCount = 4;

        private readonly float timeLimitSeconds;
        private readonly float answerLockoutSeconds;
        private float elapsedSeconds;
        private float answerableAtSeconds;

        /// <summary>Index (0-3) of the correct answer.</summary>
        public int CorrectIndex { get; }

        /// <summary>Whether the session is resolved (answered or timed out).</summary>
        public bool IsResolved { get; private set; }

        /// <summary>Result of the question; valid once IsResolved is true.</summary>
        public QuizResult Result { get; private set; }

        /// <summary>Index the player picked, or -1 if none (yet).</summary>
        public int SelectedIndex { get; private set; } = -1;

        public float RemainingTime => Math.Max(0f, timeLimitSeconds - elapsedSeconds);
        public float NormalizedTimeRemaining => RemainingTime / timeLimitSeconds;

        /// <summary>
        /// Whether an answer click would be accepted right now. False during the
        /// opening answer lockout, so the view can show the answers as not yet
        /// clickable.
        /// </summary>
        public bool IsAnswerable => elapsedSeconds >= answerableAtSeconds;

        /// <summary>
        /// Seconds still to wait before answers are accepted; zero once the
        /// session is answerable.
        /// </summary>
        public float LockoutRemaining => Math.Max(0f, answerableAtSeconds - elapsedSeconds);

        /// <summary>
        /// Creates a session for one question with the given correct index, time
        /// limit, and opening answer lockout. A lockout of zero accepts answers
        /// immediately.
        /// </summary>
        public QuizSession(int correctIndex, float timeLimitSeconds, float answerLockoutSeconds = 0f)
        {
            if (correctIndex < 0 || correctIndex >= AnswerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(correctIndex));
            }
            if (timeLimitSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeLimitSeconds));
            }
            if (answerLockoutSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(answerLockoutSeconds));
            }

            CorrectIndex = correctIndex;
            this.timeLimitSeconds = timeLimitSeconds;
            this.answerLockoutSeconds = answerLockoutSeconds;
            answerableAtSeconds = answerLockoutSeconds;
        }

        /// <summary>
        /// Selects the answer at the given index, resolving the session to
        /// Correct or Wrong. Ignored once resolved or when out of range.
        ///
        /// A selection made during the answer lockout resolves nothing and
        /// pushes the lockout out by another full window, so a player mashing
        /// an answer slot never reaches an answerable question.
        /// </summary>
        public void SelectAnswer(int index)
        {
            if (IsResolved || index < 0 || index >= AnswerCount)
            {
                return;
            }

            if (!IsAnswerable)
            {
                answerableAtSeconds = elapsedSeconds + answerLockoutSeconds;
                return;
            }

            SelectedIndex = index;
            if (index == CorrectIndex)
            {
                Resolve(QuizResult.Correct);
            }
            else
            {
                Resolve(QuizResult.Wrong);
            }
        }

        /// <summary>
        /// Ticks the countdown by deltaSeconds; at zero the session resolves
        /// as TimedOut. Ignored once resolved.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (IsResolved || deltaSeconds <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            if (RemainingTime <= 0f)
            {
                Resolve(QuizResult.TimedOut);
            }
        }

        private void Resolve(QuizResult result)
        {
            IsResolved = true;
            Result = result;
        }
    }
}
