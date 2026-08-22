using NUnit.Framework;
using UnityEngine;

namespace ThinkFast.Quiz.Tests
{
    public class QuizQuestionTests
    {
        [Test]
        public void OnValidate_RepairsOutOfRangeValues()
        {
            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.answers = new[] { "a", "b" };
            question.correctIndex = 7;
            question.timeLimitSeconds = -3f;

            question.OnValidate();

            Assert.That(question.answers.Length, Is.EqualTo(QuizQuestion.AnswerCount));
            Assert.That(question.correctIndex, Is.EqualTo(QuizQuestion.AnswerCount - 1));
            Assert.That(question.timeLimitSeconds, Is.EqualTo(1f));

            Object.DestroyImmediate(question);
        }

        [Test]
        public void OnValidate_LeavesValidValuesAlone()
        {
            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.answers = new[] { "a", "b", "c", "d" };
            question.correctIndex = 1;
            question.timeLimitSeconds = 12f;

            question.OnValidate();

            Assert.That(question.answers, Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(question.correctIndex, Is.EqualTo(1));
            Assert.That(question.timeLimitSeconds, Is.EqualTo(12f));

            Object.DestroyImmediate(question);
        }
    }
}
