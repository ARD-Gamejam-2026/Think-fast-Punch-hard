using NUnit.Framework;

namespace ThinkFast.Quiz.Tests
{
    public class QuizSessionTests
    {
        [Test]
        public void SelectAnswer_CorrectIndex_ResolvesCorrect()
        {
            var session = new QuizSession(correctIndex: 2, timeLimitSeconds: 10f);

            session.SelectAnswer(2);

            Assert.That(session.IsResolved, Is.True);
            Assert.That(session.Result, Is.EqualTo(QuizResult.Correct));
            Assert.That(session.SelectedIndex, Is.EqualTo(2));
        }

        [Test]
        public void SelectAnswer_WrongIndex_ResolvesWrong()
        {
            var session = new QuizSession(correctIndex: 2, timeLimitSeconds: 10f);

            session.SelectAnswer(0);

            Assert.That(session.IsResolved, Is.True);
            Assert.That(session.Result, Is.EqualTo(QuizResult.Wrong));
            Assert.That(session.SelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void Tick_PastTimeLimit_ResolvesTimedOut()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 1f);

            session.Tick(0.6f);
            Assert.That(session.IsResolved, Is.False);

            session.Tick(0.6f);
            Assert.That(session.IsResolved, Is.True);
            Assert.That(session.Result, Is.EqualTo(QuizResult.TimedOut));
            Assert.That(session.SelectedIndex, Is.EqualTo(-1));
        }

        [Test]
        public void SelectAnswer_JustBeforeTimeout_StillResolvesFromClick()
        {
            var session = new QuizSession(correctIndex: 1, timeLimitSeconds: 1f);

            session.Tick(0.99f);
            session.SelectAnswer(1);

            Assert.That(session.Result, Is.EqualTo(QuizResult.Correct));
        }

        [Test]
        public void SelectAnswer_AfterResolution_IsIgnored()
        {
            var session = new QuizSession(correctIndex: 1, timeLimitSeconds: 10f);

            session.SelectAnswer(0);
            session.SelectAnswer(1);

            Assert.That(session.Result, Is.EqualTo(QuizResult.Wrong));
            Assert.That(session.SelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void Tick_AfterResolution_ChangesNothing()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 10f);

            session.SelectAnswer(0);
            float remainingBefore = session.RemainingTime;
            session.Tick(5f);

            Assert.That(session.Result, Is.EqualTo(QuizResult.Correct));
            Assert.That(session.RemainingTime, Is.EqualTo(remainingBefore));
        }

        [Test]
        public void NormalizedTimeRemaining_GoesFromOneToZero()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 2f);

            Assert.That(session.NormalizedTimeRemaining, Is.EqualTo(1f));

            session.Tick(1f);
            Assert.That(session.NormalizedTimeRemaining, Is.EqualTo(0.5f).Within(1e-4));

            session.Tick(1f);
            Assert.That(session.NormalizedTimeRemaining, Is.Zero);
        }

        [Test]
        public void SelectAnswer_OutOfRangeIndex_IsIgnored()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 10f);

            session.SelectAnswer(-1);
            session.SelectAnswer(4);

            Assert.That(session.IsResolved, Is.False);
        }

        [Test]
        public void Constructor_InvalidArguments_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new QuizSession(-1, 10f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new QuizSession(4, 10f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new QuizSession(0, 0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new QuizSession(0, 10f, -1f));
        }

        [Test]
        public void NoLockout_IsAnswerableImmediately()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 10f);

            Assert.That(session.IsAnswerable, Is.True);
            Assert.That(session.LockoutRemaining, Is.Zero);
        }

        [Test]
        public void SelectAnswer_DuringLockout_DoesNotResolve()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 10f, answerLockoutSeconds: 0.35f);

            session.SelectAnswer(0);

            Assert.That(session.IsResolved, Is.False);
            Assert.That(session.SelectedIndex, Is.EqualTo(-1));
        }

        [Test]
        public void SelectAnswer_AfterLockoutElapses_ResolvesNormally()
        {
            var session = new QuizSession(correctIndex: 2, timeLimitSeconds: 10f, answerLockoutSeconds: 0.35f);

            session.Tick(0.4f);
            Assert.That(session.IsAnswerable, Is.True);

            session.SelectAnswer(2);

            Assert.That(session.Result, Is.EqualTo(QuizResult.Correct));
        }

        [Test]
        public void SelectAnswer_DuringLockout_PushesTheWindowBack()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 10f, answerLockoutSeconds: 0.35f);

            session.Tick(0.3f);
            session.SelectAnswer(0);

            // Without the push-back the window would have opened at 0.35s; the
            // click at 0.3s moves it out to 0.65s.
            session.Tick(0.1f);
            Assert.That(session.IsAnswerable, Is.False);
            Assert.That(session.LockoutRemaining, Is.EqualTo(0.25f).Within(1e-4));
        }

        [Test]
        public void SustainedMashing_NeverResolvesAndTimesOut()
        {
            var session = new QuizSession(correctIndex: 0, timeLimitSeconds: 5f, answerLockoutSeconds: 0.35f);

            // A masher clicking the correct slot every 0.1s for the whole
            // countdown: the window is re-armed faster than it can ever open.
            for (int tick = 0; tick < 60; tick++)
            {
                session.Tick(0.1f);
                session.SelectAnswer(0);
            }

            Assert.That(session.Result, Is.EqualTo(QuizResult.TimedOut));
            Assert.That(session.SelectedIndex, Is.EqualTo(-1));
        }

        [Test]
        public void OneStrayClick_CostsOnlyOneMoreWindow()
        {
            var session = new QuizSession(correctIndex: 1, timeLimitSeconds: 10f, answerLockoutSeconds: 0.35f);

            session.Tick(0.2f);
            session.SelectAnswer(1);
            session.Tick(0.4f);

            // The stray click pushed the window out to 0.55s; by 0.6s a real
            // answer lands. Reading fast is not what gets punished, mashing is.
            session.SelectAnswer(1);

            Assert.That(session.Result, Is.EqualTo(QuizResult.Correct));
        }
    }
}
