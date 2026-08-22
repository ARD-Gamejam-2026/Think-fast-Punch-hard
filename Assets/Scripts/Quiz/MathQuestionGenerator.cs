using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Builds runtime QuizQuestion instances with random small-number
    /// arithmetic (+, −, ×). The wrong answers are near misses around the
    /// correct result. Seedable for deterministic tests.
    /// </summary>
    public class MathQuestionGenerator
    {
        private readonly System.Random random;

        public float TimeLimitSeconds { get; set; } = 5f;

        /// <summary>Creates a generator; pass a seed for deterministic output.</summary>
        public MathQuestionGenerator(int? seed = null)
        {
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>Builds a fresh runtime question with a random +, − or × problem.</summary>
        public QuizQuestion Next()
        {
            int a, b, result;
            char symbol;
            switch (random.Next(3))
            {
                case 0:
                    a = random.Next(2, 100);
                    b = random.Next(2, 100);
                    result = a + b;
                    symbol = '+';
                    break;
                case 1:
                    a = random.Next(2, 100);
                    b = random.Next(2, 100);
                    if (b > a)
                    {
                        (a, b) = (b, a);
                    }
                    result = a - b;
                    symbol = '−';
                    break;
                default:
                    a = random.Next(2, 13);
                    b = random.Next(2, 13);
                    result = a * b;
                    symbol = '×';
                    break;
            }

            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.name = $"Math {a} {symbol} {b}";
            question.questionText = $"Think fast! {a} {symbol} {b} = ?";
            question.answers = BuildAnswers(result, out int correctIndex);
            question.correctIndex = correctIndex;
            question.timeLimitSeconds = TimeLimitSeconds;
            return question;
        }

        private string[] BuildAnswers(int result, out int correctIndex)
        {
            var values = new List<int> { result };
            while (values.Count < QuizQuestion.AnswerCount)
            {
                int offset = random.Next(1, 11) * (random.Next(2) == 0 ? -1 : 1);
                int candidate = result + offset;
                if (candidate >= 0 && !values.Contains(candidate))
                {
                    values.Add(candidate);
                }
            }

            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            correctIndex = values.IndexOf(result);
            return values.Select(value => value.ToString()).ToArray();
        }
    }
}
