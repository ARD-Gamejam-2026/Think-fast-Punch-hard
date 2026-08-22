using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ThinkFast.Quiz;

namespace ThinkFast.Quiz.Tests
{
    public class ShapeRendererTests
    {
        [Test]
        public void Palette_size_matches_generator_expectation()
        {
            Assert.AreEqual(4, new ShapeRenderer().PaletteSize);
        }

        [Test]
        public void Filled_square_is_opaque_at_center_and_clear_at_corner()
        {
            var renderer = new ShapeRenderer();
            Sprite sprite = renderer.Render(new ShapeSpec(ShapeKind.Square, 0, 1, 0, true));
            Texture2D texture = sprite.texture;
            int w = texture.width;
            int h = texture.height;

            Color center = texture.GetPixel(w / 2, h / 2);
            Color corner = texture.GetPixel(1, 1);
            Assert.Greater(center.a, 0.5f, "center of a filled square must be opaque");
            Assert.Less(corner.a, 0.5f, "corner must be transparent");

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void Render_strip_lays_terms_in_two_rows_with_a_placeholder_row()
        {
            var renderer = new ShapeRenderer();

            Sprite six = renderer.RenderStrip(CircleTerms(6));
            int sixCell = six.texture.height / 3;
            Assert.AreEqual(sixCell * 3, six.texture.width, "six terms use three columns");
            Assert.AreEqual(sixCell * 3, six.texture.height, "two term rows plus a placeholder row");
            Object.DestroyImmediate(six.texture);
            Object.DestroyImmediate(six);

            Sprite four = renderer.RenderStrip(CircleTerms(4));
            int fourCell = four.texture.height / 3;
            Assert.AreEqual(fourCell * 2, four.texture.width, "four terms use two columns");
            Assert.Greater(four.texture.height, four.texture.width, "placeholder row makes it taller than wide");
            Object.DestroyImmediate(four.texture);
            Object.DestroyImmediate(four);
        }

        private static List<ShapeSpec> CircleTerms(int count)
        {
            var terms = new List<ShapeSpec>();
            for (int i = 0; i < count; i++)
            {
                terms.Add(new ShapeSpec(ShapeKind.Circle, 0, 1, 0, true));
            }
            return terms;
        }

        [Test]
        public void Render_every_kind_rotation_count_and_fill_without_throwing()
        {
            var renderer = new ShapeRenderer();
            var kinds = new[] { ShapeKind.Circle, ShapeKind.Square, ShapeKind.Triangle, ShapeKind.Star };
            var rotations = new[] { 0, 45, 90, 135 };
            var counts = new[] { 1, 4, 9 };
            var fills = new[] { true, false };

            foreach (var kind in kinds)
            {
                foreach (var rotation in rotations)
                {
                    foreach (var count in counts)
                    {
                        foreach (var filled in fills)
                        {
                            RenderAndAssertSize(renderer, kind, rotation, count, filled);
                        }
                    }
                }
            }
        }

        private static void RenderAndAssertSize(ShapeRenderer renderer, ShapeKind kind, int rotation, int count, bool filled)
        {
            var spec = new ShapeSpec(kind, rotation, count, 0, filled);
            Sprite sprite = null;
            Texture2D texture = null;
            try
            {
                Assert.DoesNotThrow(() => sprite = renderer.Render(spec));
                texture = sprite.texture;
                Assert.AreEqual(128, texture.width);
                Assert.AreEqual(128, texture.height);
            }
            finally
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
                if (sprite != null)
                {
                    Object.DestroyImmediate(sprite);
                }
            }
        }
    }
}
