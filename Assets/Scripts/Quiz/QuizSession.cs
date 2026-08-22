using System;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Rules of a single question in progress. Plain C#, no scene dependencies,
    /// so it is fully covered by EditMode tests. Resolves exactly once to
    /// Correct, Wrong, or TimedOut; all input is ignored afterwards.
    /// </summary>
    public class QuizSession
    {
        public const int AnswerCount = 4;

        private readonly float timeLimitSeconds;
        private float elapsedSeconds;

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

        /// <summary>Creates a session for one question with the given correct index and time limit.</summary>
        public QuizSession(int correctIndex, float timeLimitSeconds)
        {
            if (correctIndex < 0 || correctIndex >= AnswerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(correctIndex));
            }
            if (timeLimitSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeLimitSeconds));
            }

            CorrectIndex = correctIndex;
            this.timeLimitSeconds = timeLimitSeconds;
        }

        /// <summary>
        /// Selects the answer at the given index, resolving the session to
        /// Correct or Wrong. Ignored once resolved or when out of range.
        /// </summary>
        public void SelectAnswer(int index)
        {
            if (IsResolved || index < 0 || index >= AnswerCount)
            {
                return;
            }

            SelectedIndex = index;
            Resolve(index == CorrectIndex ? QuizResult.Correct : QuizResult.Wrong);
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
