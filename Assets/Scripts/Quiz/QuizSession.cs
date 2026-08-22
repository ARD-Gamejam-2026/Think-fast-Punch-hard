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

        public int CorrectIndex { get; }
        public bool IsResolved { get; private set; }
        public QuizResult Result { get; private set; }
        public int SelectedIndex { get; private set; } = -1;

        public float RemainingTime => Math.Max(0f, timeLimitSeconds - elapsedSeconds);
        public float NormalizedTimeRemaining => RemainingTime / timeLimitSeconds;

        public QuizSession(int correctIndex, float timeLimitSeconds)
        {
            if (correctIndex < 0 || correctIndex >= AnswerCount)
                throw new ArgumentOutOfRangeException(nameof(correctIndex));
            if (timeLimitSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeLimitSeconds));

            CorrectIndex = correctIndex;
            this.timeLimitSeconds = timeLimitSeconds;
        }

        public void SelectAnswer(int index)
        {
            if (IsResolved || index < 0 || index >= AnswerCount)
                return;

            SelectedIndex = index;
            Resolve(index == CorrectIndex ? QuizResult.Correct : QuizResult.Wrong);
        }

        public void Tick(float deltaSeconds)
        {
            if (IsResolved || deltaSeconds <= 0f)
                return;

            elapsedSeconds += deltaSeconds;
            if (RemainingTime <= 0f)
                Resolve(QuizResult.TimedOut);
        }

        private void Resolve(QuizResult result)
        {
            IsResolved = true;
            Result = result;
        }
    }
}
