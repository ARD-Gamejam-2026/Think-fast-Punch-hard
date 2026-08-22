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
        public void Render_strip_produces_a_wider_than_tall_sprite()
        {
            var renderer = new ShapeRenderer();
            var terms = new List<ShapeSpec>
            {
                new ShapeSpec(ShapeKind.Circle, 0, 1, 0, true),
                new ShapeSpec(ShapeKind.Circle, 0, 2, 0, true),
                new ShapeSpec(ShapeKind.Circle, 0, 3, 0, true),
            };
            Sprite strip = renderer.RenderStrip(terms);
            Assert.Greater(strip.texture.width, strip.texture.height);

            Object.DestroyImmediate(strip.texture);
            Object.DestroyImmediate(strip);
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
