using NUnit.Framework;
using UnityEngine;
using ThinkFast.Quiz;

namespace ThinkFast.Quiz.Tests
{
    public class SequenceQuestionGeneratorTests
    {
        [Test]
        public void Number_question_has_four_text_answers_and_no_images()
        {
            var generator = new SequenceQuestionGenerator(seed: 1) { ShapeShare = 0f };
            QuizQuestion question = generator.Next();
            Assert.AreEqual(QuizQuestion.AnswerCount, question.answers.Length);
            Assert.IsNull(question.answerImages);
            Assert.GreaterOrEqual(question.correctIndex, 0);
            Assert.Less(question.correctIndex, QuizQuestion.AnswerCount);
            Assert.AreEqual(8f, question.timeLimitSeconds);
            CleanUp(question);
        }

        [Test]
        public void Shape_question_has_strip_image_and_four_answer_images()
        {
            var generator = new SequenceQuestionGenerator(seed: 2) { ShapeShare = 1f };
            QuizQuestion question = generator.Next();
            Assert.IsNotNull(question.image);
            Assert.IsNotNull(question.answerImages);
            Assert.AreEqual(QuizQuestion.AnswerCount, question.answerImages.Length);
            foreach (var sprite in question.answerImages)
            {
                Assert.IsNotNull(sprite);
            }
            CleanUp(question);
        }

        private static void CleanUp(QuizQuestion question)
        {
            Object.DestroyImmediate(question);
        }
    }
}
