using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThinkFast.Quiz.Tests
{
    public class DifficultyRampTests
    {
        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            info.SetValue(target, value);
        }

        private static void Resolve(DifficultyRamp ramp, QuizResult result)
        {
            MethodInfo m = typeof(DifficultyRamp).GetMethod("HandleResolved", BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(ramp, new object[] { result, 1f });
        }

        private static DifficultyRamp NewRamp(out GameObject go)
        {
            go = new GameObject("ramp");
            var ramp = go.AddComponent<DifficultyRamp>();
            SetPrivate(ramp, "correctAnswersToMax", 4);
            SetPrivate(ramp, "rampCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return ramp;
        }

        [SetUp]
        public void SetUp()
        {
            // A DifficultyRamp with no QuizController may log on its lifecycle
            // callbacks; these tests drive the handler directly and do not
            // exercise that wiring, so a stray log must not fail the test.
            LogAssert.ignoreFailingMessages = true;
            DifficultyRamp.ResetRamp();
        }

        [TearDown]
        public void TearDown()
        {
            DifficultyRamp.ResetRamp();
        }

        [Test]
        public void Current01_RisesWithCorrectAnswers()
        {
            var ramp = NewRamp(out var go);

            Resolve(ramp, QuizResult.Correct);
            Resolve(ramp, QuizResult.Correct);
            Assert.That(DifficultyRamp.Current01, Is.EqualTo(0.5f).Within(1e-4f));

            Resolve(ramp, QuizResult.Correct);
            Resolve(ramp, QuizResult.Correct);
            Assert.That(DifficultyRamp.Current01, Is.EqualTo(1f).Within(1e-4f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Current01_ClampsAtOneBeyondTarget()
        {
            var ramp = NewRamp(out var go);

            for (int i = 0; i < 6; i++)
            {
                Resolve(ramp, QuizResult.Correct);
            }
            Assert.That(DifficultyRamp.Current01, Is.EqualTo(1f).Within(1e-4f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void WrongAndTimeout_DoNotRaiseDifficulty()
        {
            var ramp = NewRamp(out var go);

            Resolve(ramp, QuizResult.Wrong);
            Resolve(ramp, QuizResult.TimedOut);
            Assert.That(DifficultyRamp.Current01, Is.EqualTo(0f).Within(1e-4f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetRamp_ReturnsToZero()
        {
            var ramp = NewRamp(out var go);

            Resolve(ramp, QuizResult.Correct);
            Assert.That(DifficultyRamp.Current01, Is.GreaterThan(0f));

            DifficultyRamp.ResetRamp();
            Assert.That(DifficultyRamp.Current01, Is.EqualTo(0f).Within(1e-4f));

            Object.DestroyImmediate(go);
        }
    }
}
