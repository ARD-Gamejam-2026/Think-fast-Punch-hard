using System.Collections.Generic;
using NUnit.Framework;
using ThinkFast.Quiz;

namespace ThinkFast.Quiz.Tests
{
    public class SequenceGeneratorTests
    {
        [Test]
        public void ShapeSpec_normalizes_rotation_into_0_359()
        {
            var spec = new ShapeSpec(ShapeKind.Triangle, 450, 1, 0, true);
            Assert.AreEqual(90, spec.RotationDegrees);

            var negative = new ShapeSpec(ShapeKind.Triangle, -90, 1, 0, true);
            Assert.AreEqual(270, negative.RotationDegrees);
        }

        [Test]
        public void ShapeSpec_value_equality_compares_all_fields()
        {
            var a = new ShapeSpec(ShapeKind.Star, 90, 2, 1, true);
            var b = new ShapeSpec(ShapeKind.Star, 90, 2, 1, true);
            var c = new ShapeSpec(ShapeKind.Star, 90, 2, 1, false);

            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
            Assert.AreNotEqual(a, c);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void Number_options_are_four_distinct_with_valid_correct_index()
        {
            var generator = new SequenceGenerator(seed: 1234);
            for (int i = 0; i < 50; i++)
            {
                NumberPuzzle puzzle = generator.NextNumber();
                Assert.AreEqual(4, puzzle.Options.Length);
                CollectionAssert.AllItemsAreUnique(puzzle.Options);
                Assert.GreaterOrEqual(puzzle.CorrectIndex, 0);
                Assert.Less(puzzle.CorrectIndex, 4);
                Assert.GreaterOrEqual(puzzle.Terms.Length, 4);
            }
        }

        [Test]
        public void Number_generator_is_deterministic_for_a_seed()
        {
            var a = new SequenceGenerator(seed: 42).NextNumber();
            var b = new SequenceGenerator(seed: 42).NextNumber();
            CollectionAssert.AreEqual(a.Terms, b.Terms);
            CollectionAssert.AreEqual(a.Options, b.Options);
            Assert.AreEqual(a.CorrectIndex, b.CorrectIndex);
        }

        [Test]
        public void Every_number_family_places_a_solvable_next_term()
        {
            // With enough samples, all four families appear; each must have the
            // correct next term present exactly once among the options.
            var generator = new SequenceGenerator(seed: 7);
            for (int i = 0; i < 200; i++)
            {
                NumberPuzzle puzzle = generator.NextNumber();
                string correct = puzzle.Options[puzzle.CorrectIndex];
                int occurrences = System.Array.FindAll(puzzle.Options, o => o == correct).Length;
                Assert.AreEqual(1, occurrences, "correct term must be unique among options");
            }
        }

        [Test]
        public void Shape_options_are_four_distinct_with_valid_correct_index()
        {
            var generator = new SequenceGenerator(seed: 99);
            for (int i = 0; i < 100; i++)
            {
                ShapePuzzle puzzle = generator.NextShape();
                Assert.AreEqual(4, puzzle.Options.Length);
                CollectionAssert.AllItemsAreUnique(puzzle.Options);
                Assert.GreaterOrEqual(puzzle.CorrectIndex, 0);
                Assert.Less(puzzle.CorrectIndex, 4);
                Assert.GreaterOrEqual(puzzle.Terms.Length, 4);
            }
        }

        [Test]
        public void Shape_generator_is_deterministic_for_a_seed()
        {
            var a = new SequenceGenerator(seed: 5).NextShape();
            var b = new SequenceGenerator(seed: 5).NextShape();
            CollectionAssert.AreEqual(a.Terms, b.Terms);
            CollectionAssert.AreEqual(a.Options, b.Options);
            Assert.AreEqual(a.CorrectIndex, b.CorrectIndex);
        }

        [Test]
        public void Shape_color_index_stays_within_palette()
        {
            var generator = new SequenceGenerator(seed: 3);
            for (int i = 0; i < 100; i++)
            {
                ShapePuzzle puzzle = generator.NextShape();
                foreach (var spec in puzzle.Options)
                {
                    Assert.GreaterOrEqual(spec.ColorIndex, 0);
                    Assert.Less(spec.ColorIndex, 4);
                    Assert.GreaterOrEqual(spec.Count, 1);
                }
            }
        }

        [Test]
        public void Rotation_puzzles_avoid_symmetric_kinds_and_options_stay_visually_distinct()
        {
            int rotationPuzzlesChecked = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                var generator = new SequenceGenerator(seed: seed * 97 + 1);
                for (int i = 0; i < 200; i++)
                {
                    ShapePuzzle puzzle = generator.NextShape();
                    if (!IsRotationPuzzle(puzzle))
                    {
                        continue;
                    }
                    rotationPuzzlesChecked++;
                    AssertRotationPuzzleIsSolvable(puzzle);
                }
            }
            Assert.Greater(rotationPuzzlesChecked, 0, "expected at least one rotation puzzle across the sampled seeds");
        }

        private static void AssertRotationPuzzleIsSolvable(ShapePuzzle puzzle)
        {
            ShapeKind kind = puzzle.Terms[0].Kind;
            Assert.AreNotEqual(ShapeKind.Square, kind,
                "square has 90-degree rotational symmetry and collides with 45/90 degree rotation steps");
            Assert.AreNotEqual(ShapeKind.Star, kind,
                "star is near-symmetric and its rotated steps are not visually distinguishable");

            int period = RotationalSymmetryPeriod(kind);
            var reducedRotations = new HashSet<int>();
            foreach (var option in puzzle.Options)
            {
                int reduced = ((option.RotationDegrees % period) + period) % period;
                reducedRotations.Add(reduced);
            }
            Assert.AreEqual(4, reducedRotations.Count,
                "rotation options must render as four visually distinct shapes");
        }

        private static bool IsRotationPuzzle(ShapePuzzle puzzle)
        {
            ShapeSpec first = puzzle.Terms[0];
            var distinctRotations = new HashSet<int>();
            foreach (var term in puzzle.Terms)
            {
                bool sameShape = term.Kind == first.Kind
                    && term.Count == first.Count
                    && term.ColorIndex == first.ColorIndex
                    && term.Filled == first.Filled;
                if (!sameShape)
                {
                    return false;
                }
                distinctRotations.Add(term.RotationDegrees);
            }
            return distinctRotations.Count >= 2;
        }

        private static int RotationalSymmetryPeriod(ShapeKind kind)
        {
            switch (kind)
            {
                case ShapeKind.Triangle:
                    return 120;
                case ShapeKind.Star:
                    return 72;
                default:
                    return 360;
            }
        }
    }
}
