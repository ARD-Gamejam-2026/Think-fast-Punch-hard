using System.Collections.Generic;
using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Builds number and shape sequence puzzles procedurally. Pure logic (no
    /// rendering), seedable for deterministic tests.
    /// </summary>
    public sealed class SequenceGenerator
    {
        private const int OptionCount = QuizQuestion.AnswerCount;
        private const int PaletteSize = 4;

        private static readonly ShapeKind[] AllKinds =
        {
            ShapeKind.Circle, ShapeKind.Square, ShapeKind.Triangle, ShapeKind.Star,
        };

        private static readonly ShapeKind[] OrientedKinds =
        {
            ShapeKind.Triangle, ShapeKind.Star, ShapeKind.Square,
        };

        private readonly System.Random random;

        /// <summary>Creates a generator; pass a seed for deterministic output.</summary>
        public SequenceGenerator(int? seed = null)
        {
            if (seed.HasValue)
            {
                random = new System.Random(seed.Value);
            }
            else
            {
                random = new System.Random();
            }
        }

        /// <summary>Generates the next number puzzle from a random family.</summary>
        public NumberPuzzle NextNumber()
        {
            switch (random.Next(4))
            {
                case 0:
                    return Arithmetic();
                case 1:
                    return Geometric();
                case 2:
                    return Fibonacci();
                default:
                    return Alternating();
            }
        }

        /// <summary>Generates the next shape puzzle from a random transformation.</summary>
        public ShapePuzzle NextShape()
        {
            switch (random.Next(4))
            {
                case 0:
                    return RotationPuzzle();
                case 1:
                    return CountPuzzle();
                case 2:
                    return ShapeCyclePuzzle();
                default:
                    return ColorCyclePuzzle();
            }
        }

        private ShapePuzzle RotationPuzzle()
        {
            ShapeKind kind = OrientedKinds[random.Next(OrientedKinds.Length)];
            int color = random.Next(PaletteSize);
            int step = 90;
            if (random.Next(2) == 0)
            {
                step = 45;
            }
            if (random.Next(2) == 0)
            {
                step = -step;
            }
            int start = random.Next(4) * 90;
            var terms = new ShapeSpec[4];
            for (int i = 0; i < 4; i++)
            {
                terms[i] = new ShapeSpec(kind, start + step * i, 1, color, true);
            }
            var correct = new ShapeSpec(kind, start + step * 4, 1, color, true);
            var wrong = new List<ShapeSpec>
            {
                new ShapeSpec(kind, start + step * 3, 1, color, true),
                new ShapeSpec(kind, start + step * 5, 1, color, true),
                new ShapeSpec(kind, start - step * 2, 1, color, true),
            };
            return BuildShapePuzzle(terms, correct, wrong);
        }

        private ShapePuzzle CountPuzzle()
        {
            ShapeKind kind = AllKinds[random.Next(AllKinds.Length)];
            int color = random.Next(PaletteSize);
            int step = random.Next(1, 3);
            int start = random.Next(1, 3);
            var terms = new ShapeSpec[4];
            for (int i = 0; i < 4; i++)
            {
                terms[i] = new ShapeSpec(kind, 0, start + step * i, color, true);
            }
            int nextCount = start + step * 4;
            var correct = new ShapeSpec(kind, 0, nextCount, color, true);
            var wrong = new List<ShapeSpec>
            {
                new ShapeSpec(kind, 0, nextCount - 1, color, true),
                new ShapeSpec(kind, 0, nextCount + 1, color, true),
                new ShapeSpec(kind, 0, nextCount + 2, color, true),
            };
            return BuildShapePuzzle(terms, correct, wrong);
        }

        private ShapePuzzle ShapeCyclePuzzle()
        {
            int color = random.Next(PaletteSize);
            int cycleLength = random.Next(2, 4);
            ShapeKind[] cycle = DistinctKinds(cycleLength);
            var terms = new ShapeSpec[6];
            for (int i = 0; i < 6; i++)
            {
                terms[i] = new ShapeSpec(cycle[i % cycle.Length], 0, 1, color, true);
            }
            ShapeKind nextKind = cycle[6 % cycle.Length];
            var correct = new ShapeSpec(nextKind, 0, 1, color, true);
            var wrong = new List<ShapeSpec>();
            foreach (var kind in AllKinds)
            {
                if (kind != nextKind)
                {
                    wrong.Add(new ShapeSpec(kind, 0, 1, color, true));
                }
            }
            return BuildShapePuzzle(terms, correct, wrong);
        }

        private ShapePuzzle ColorCyclePuzzle()
        {
            ShapeKind kind = AllKinds[random.Next(AllKinds.Length)];
            int cycleLength = random.Next(2, 4);
            int[] cycle = DistinctColors(cycleLength);
            var terms = new ShapeSpec[6];
            for (int i = 0; i < 6; i++)
            {
                int color = cycle[i % cycle.Length];
                terms[i] = new ShapeSpec(kind, 0, 1, color, color % 2 == 0);
            }
            int nextColor = cycle[6 % cycle.Length];
            var correct = new ShapeSpec(kind, 0, 1, nextColor, nextColor % 2 == 0);
            var wrong = new List<ShapeSpec>();
            for (int c = 0; c < PaletteSize; c++)
            {
                if (c != nextColor)
                {
                    wrong.Add(new ShapeSpec(kind, 0, 1, c, c % 2 == 0));
                }
            }
            return BuildShapePuzzle(terms, correct, wrong);
        }

        private ShapeKind[] DistinctKinds(int count)
        {
            var pool = new List<ShapeKind>(AllKinds);
            Shuffle(pool);
            return pool.GetRange(0, count).ToArray();
        }

        private int[] DistinctColors(int count)
        {
            var pool = new List<int>();
            for (int c = 0; c < PaletteSize; c++)
            {
                pool.Add(c);
            }
            Shuffle(pool);
            return pool.GetRange(0, count).ToArray();
        }

        private ShapePuzzle BuildShapePuzzle(ShapeSpec[] terms, ShapeSpec correct, List<ShapeSpec> wrong)
        {
            var options = new List<ShapeSpec> { correct };
            foreach (var candidate in wrong)
            {
                if (options.Count >= OptionCount)
                {
                    break;
                }
                if (!options.Contains(candidate))
                {
                    options.Add(candidate);
                }
            }
            Shuffle(options);
            return new ShapePuzzle
            {
                Terms = terms,
                Options = options.ToArray(),
                CorrectIndex = options.IndexOf(correct),
            };
        }

        private NumberPuzzle Arithmetic()
        {
            int step = random.Next(2, 10);
            bool descending = random.Next(2) == 0;
            if (descending)
            {
                step = -step;
            }
            int start = Mathf.Abs(step) * 4 + random.Next(0, 20);
            var terms = new int[4];
            for (int i = 0; i < 4; i++)
            {
                terms[i] = start + step * i;
            }
            return BuildNumberPuzzle(terms, start + step * 4);
        }

        private NumberPuzzle Geometric()
        {
            int ratio = random.Next(2, 4);
            int start = random.Next(1, 5);
            var terms = new int[4];
            int value = start;
            for (int i = 0; i < 4; i++)
            {
                terms[i] = value;
                value *= ratio;
            }
            return BuildNumberPuzzle(terms, value);
        }

        private NumberPuzzle Fibonacci()
        {
            var terms = new int[5];
            terms[0] = random.Next(1, 6);
            terms[1] = terms[0] + random.Next(1, 6);
            for (int i = 2; i < 5; i++)
            {
                terms[i] = terms[i - 1] + terms[i - 2];
            }
            return BuildNumberPuzzle(terms, terms[4] + terms[3]);
        }

        private NumberPuzzle Alternating()
        {
            int startA = random.Next(1, 10);
            int stepA = random.Next(1, 6);
            int startB = random.Next(10, 30);
            int stepB = random.Next(5, 12);
            var terms = new int[5];
            terms[0] = startA;
            terms[1] = startB;
            terms[2] = startA + stepA;
            terms[3] = startB + stepB;
            terms[4] = startA + stepA * 2;
            return BuildNumberPuzzle(terms, startB + stepB * 2);
        }

        private NumberPuzzle BuildNumberPuzzle(int[] terms, int correct)
        {
            int[] values = NearMissOptions(correct);
            int correctIndex = System.Array.IndexOf(values, correct);
            var options = new string[OptionCount];
            for (int i = 0; i < OptionCount; i++)
            {
                options[i] = values[i].ToString();
            }
            return new NumberPuzzle
            {
                Terms = terms,
                Options = options,
                CorrectIndex = correctIndex,
            };
        }

        private int[] NearMissOptions(int correct)
        {
            int spread = Mathf.Max(1, Mathf.Abs(correct) / 10);
            var values = new List<int> { correct };
            int guard = 0;
            while (values.Count < OptionCount && guard < 200)
            {
                guard++;
                int offset = random.Next(1, spread + 3);
                if (random.Next(2) == 0)
                {
                    offset = -offset;
                }
                int candidate = correct + offset;
                if (candidate >= 0 && !values.Contains(candidate))
                {
                    values.Add(candidate);
                }
            }
            Shuffle(values);
            return values.ToArray();
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
