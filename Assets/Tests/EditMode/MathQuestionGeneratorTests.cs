using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ThinkFast.Quiz.Tests
{
    public class MathQuestionGeneratorTests
    {
        private static readonly Regex QuestionPattern =
            new Regex(@"^Think fast! (\d+) ([+−×]) (\d+) = \?$");

        [Test]
        public void Next_GeneratesValidQuestions()
        {
            var generator = new MathQuestionGenerator(seed: 12345);

            for (int i = 0; i < 200; i++)
            {
                var question = generator.Next();

                var match = QuestionPattern.Match(question.questionText);
                Assert.That(match.Success, Is.True,
                    $"unexpected question text: {question.questionText}");

                int a = int.Parse(match.Groups[1].Value);
                int b = int.Parse(match.Groups[3].Value);
                int expected = match.Groups[2].Value switch
                {
                    "+" => a + b,
                    "−" => a - b,
                    _ => a * b,
                };

                Assert.That(question.answers.Length, Is.EqualTo(QuizQuestion.AnswerCount));
                Assert.That(question.answers, Is.Unique);
                Assert.That(question.correctIndex, Is.InRange(0, QuizQuestion.AnswerCount - 1));
                Assert.That(question.answers[question.correctIndex],
                    Is.EqualTo(expected.ToString()));
                Assert.That(expected, Is.GreaterThanOrEqualTo(0));
                Assert.That(question.timeLimitSeconds, Is.EqualTo(5f));

                Object.DestroyImmediate(question);
            }
        }

        [Test]
        public void Next_DistractorsAreNonNegative()
        {
            var generator = new MathQuestionGenerator(seed: 42);

            for (int i = 0; i < 200; i++)
            {
                var question = generator.Next();

                foreach (string answer in question.answers)
                {
                    Assert.That(int.Parse(answer), Is.GreaterThanOrEqualTo(0));
                }

                Object.DestroyImmediate(question);
            }
        }

        [Test]
        public void Next_RespectsConfiguredTimeLimit()
        {
            var generator = new MathQuestionGenerator(seed: 7) { TimeLimitSeconds = 3f };

            var question = generator.Next();

            Assert.That(question.timeLimitSeconds, Is.EqualTo(3f));

            Object.DestroyImmediate(question);
        }
    }
}
