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
        public void Render_strip_wraps_terms_and_centers_the_placeholder_row()
        {
            var renderer = new ShapeRenderer();

            // Six terms wrap to two rows of three, plus a placeholder row: square (3x3).
            Sprite six = renderer.RenderStrip(CircleTerms(6));
            Assert.AreEqual(six.texture.height, six.texture.width, "six terms wrap to a square 3x3 layout");
            Object.DestroyImmediate(six.texture);
            Object.DestroyImmediate(six);

            // Four terms stay on one row, plus a placeholder row: wider than tall (4x2).
            Sprite four = renderer.RenderStrip(CircleTerms(4));
            Assert.Greater(four.texture.width, four.texture.height, "four terms stay on one row");
            Object.DestroyImmediate(four.texture);
            Object.DestroyImmediate(four);
        }

        [Test]
        public void Render_strip_spaces_count_sequences_but_not_single_shape_sequences()
        {
            var renderer = new ShapeRenderer();
            var single = new List<ShapeSpec>();
            var counted = new List<ShapeSpec>();
            for (int i = 0; i < 4; i++)
            {
                single.Add(new ShapeSpec(ShapeKind.Circle, 0, 1, 0, true));
                counted.Add(new ShapeSpec(ShapeKind.Circle, 0, i + 1, 0, true));
            }
            Sprite tight = renderer.RenderStrip(single);
            Sprite spaced = renderer.RenderStrip(counted);
            Assert.Greater(spaced.texture.width, tight.texture.width,
                "count sequences add gaps between cells; single-shape sequences do not");
            Object.DestroyImmediate(tight.texture);
            Object.DestroyImmediate(tight);
            Object.DestroyImmediate(spaced.texture);
            Object.DestroyImmediate(spaced);
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

            foreach (var kind in kinds)
            {
                foreach (var rotation in rotations)
                {
                    foreach (var count in counts)
                    {
                        RenderAndAssertSize(renderer, kind, rotation, count, true);
                        RenderAndAssertSize(renderer, kind, rotation, count, false);
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
