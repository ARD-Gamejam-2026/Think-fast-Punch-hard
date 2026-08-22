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
    }
}
