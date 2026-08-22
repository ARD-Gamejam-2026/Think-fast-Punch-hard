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

        [Test]
        public void Seeded_number_branch_reaches_every_family()
        {
            // The correlated-seed bug shared one RNG sample between the branch roll
            // and the family pick: Mono's Next(4) equals (int)(sample * 4). Because
            // the number branch runs when sample >= ShapeShare (0.5), it forced the
            // family into {2,3} = Fibonacci/Alternating (five shown terms), leaving
            // Arithmetic/Geometric (four shown terms) unreachable in the seeded
            // number path. Decorrelating the sequence seed frees every family, so a
            // seed sweep must surface both four-term and five-term number questions.
            bool sawFourTermQuestion = false;
            bool sawFiveTermQuestion = false;
            for (int seed = 0; seed < 200; seed++)
            {
                var generator = new SequenceQuestionGenerator(seed) { ShapeShare = 0.5f };
                QuizQuestion question = generator.Next();
                if (question.answerImages == null)
                {
                    int terms = CountShownTerms(question.questionText);
                    if (terms == 4)
                    {
                        sawFourTermQuestion = true;
                    }
                    else if (terms == 5)
                    {
                        sawFiveTermQuestion = true;
                    }
                }
                CleanUp(question);
            }
            Assert.IsTrue(sawFourTermQuestion,
                "Four-term number families (Arithmetic/Geometric) must be reachable");
            Assert.IsTrue(sawFiveTermQuestion,
                "The five-term number family (Fibonacci) must be reachable");
        }

        [Test]
        public void Same_seed_produces_identical_questions()
        {
            var first = new SequenceQuestionGenerator(seed: 7) { ShapeShare = 0.5f };
            var second = new SequenceQuestionGenerator(seed: 7) { ShapeShare = 0.5f };
            for (int i = 0; i < 20; i++)
            {
                QuizQuestion questionA = first.Next();
                QuizQuestion questionB = second.Next();
                Assert.AreEqual(questionA.questionText, questionB.questionText);
                Assert.AreEqual(questionA.correctIndex, questionB.correctIndex);
                Assert.AreEqual(questionA.answerImages == null, questionB.answerImages == null);
                CleanUp(questionA);
                CleanUp(questionB);
            }
        }

        private static int CountShownTerms(string questionText)
        {
            int newline = questionText.IndexOf('\n');
            string termsPart = questionText.Substring(newline + 1);
            string[] tokens = termsPart.Split(new[] { ", " }, System.StringSplitOptions.None);
            return tokens.Length - 1;
        }

        private static void CleanUp(QuizQuestion question)
        {
            if (question.image != null)
            {
                DestroySprite(question.image);
            }
            if (question.answerImages != null)
            {
                foreach (var sprite in question.answerImages)
                {
                    if (sprite != null)
                    {
                        DestroySprite(sprite);
                    }
                }
            }
            Object.DestroyImmediate(question);
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite.texture != null)
            {
                Object.DestroyImmediate(sprite.texture);
            }
            Object.DestroyImmediate(sprite);
        }
    }
}
