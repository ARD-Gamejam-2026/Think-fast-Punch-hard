using NUnit.Framework;
using UnityEngine;
using ThinkFast.Quiz;

namespace ThinkFast.Quiz.Tests
{
    public class QuizQuestionAnswerImagesTests
    {
        [Test]
        public void AnswerImages_default_to_null()
        {
            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            Assert.IsNull(question.answerImages);
            Object.DestroyImmediate(question);
        }

        [Test]
        public void OnValidate_resizes_non_null_answer_images_to_answer_count()
        {
            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.answers = new[] { "a", "b", "c", "d" };
            question.answerImages = new Sprite[2];
            question.OnValidate();
            Assert.AreEqual(QuizQuestion.AnswerCount, question.answerImages.Length);
            Object.DestroyImmediate(question);
        }
    }
}
