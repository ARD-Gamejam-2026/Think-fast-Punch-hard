using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Builds runtime QuizQuestion instances for the sequence quiz type,
    /// rolling between number and shape sequences. Seedable for tests.
    /// </summary>
    public sealed class SequenceQuestionGenerator
    {
        private readonly System.Random random;
        private readonly SequenceGenerator sequences;
        private readonly ShapeRenderer renderer;

        /// <summary>Countdown length applied to generated sequence questions.</summary>
        public float TimeLimitSeconds { get; set; } = 8f;

        /// <summary>Probability (0-1) that a generated question is a shape sequence.</summary>
        public float ShapeShare { get; set; } = 0.5f;

        /// <summary>Creates a generator; pass a seed for deterministic output.</summary>
        public SequenceQuestionGenerator(int? seed = null)
        {
            if (seed.HasValue)
            {
                random = new System.Random(seed.Value);
                sequences = new SequenceGenerator(seed.Value);
            }
            else
            {
                random = new System.Random();
                sequences = new SequenceGenerator();
            }
            renderer = new ShapeRenderer();
        }

        /// <summary>Generates the next sequence question, number or shape.</summary>
        public QuizQuestion Next()
        {
            if (random.NextDouble() < ShapeShare)
            {
                return BuildShapeQuestion(sequences.NextShape());
            }
            return BuildNumberQuestion(sequences.NextNumber());
        }

        private QuizQuestion BuildNumberQuestion(NumberPuzzle puzzle)
        {
            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.name = "Sequence (numbers)";
            question.questionText = $"What comes next?\n{string.Join(", ", puzzle.Terms)}, ?";
            question.answers = puzzle.Options;
            question.answerImages = null;
            question.correctIndex = puzzle.CorrectIndex;
            question.timeLimitSeconds = TimeLimitSeconds;
            return question;
        }

        private QuizQuestion BuildShapeQuestion(ShapePuzzle puzzle)
        {
            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.name = "Sequence (shapes)";
            question.questionText = "What comes next?";
            question.image = renderer.RenderStrip(puzzle.Terms);
            question.answers = new string[QuizQuestion.AnswerCount];
            question.answerImages = new Sprite[QuizQuestion.AnswerCount];
            for (int i = 0; i < QuizQuestion.AnswerCount; i++)
            {
                question.answers[i] = string.Empty;
                question.answerImages[i] = renderer.Render(puzzle.Options[i]);
            }
            question.correctIndex = puzzle.CorrectIndex;
            question.timeLimitSeconds = TimeLimitSeconds;
            return question;
        }
    }
}
