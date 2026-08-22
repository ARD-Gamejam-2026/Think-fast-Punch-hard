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
    }
}
