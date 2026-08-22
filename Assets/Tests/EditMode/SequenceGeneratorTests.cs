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
    }
}
