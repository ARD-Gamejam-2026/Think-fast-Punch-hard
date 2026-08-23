using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Generates the interface's sprites as PNG assets: a rounded card, its halo,
    /// a pill, a circle, a plain block and a boxing glove.
    ///
    /// Six shapes cover every surface in the game, because each is tinted at
    /// runtime rather than baked in a colour. Fewer shapes means the rounding
    /// stays consistent everywhere, which is most of what makes this style read as
    /// one interface rather than a pile of widgets.
    ///
    /// They are generated rather than drawn for the same reason the rest of the
    /// project is: re-runnable, reviewable as code, and no binary nobody can edit.
    /// Shapes are built from a signed distance field, so the corners are properly
    /// anti-aliased at any size instead of stair-stepping.
    /// </summary>
    public static class UiSpriteFactory
    {
        public const string Folder = "Assets/UI/Generated";

        private const string CardPath = Folder + "/Card.png";
        private const string CardGlowPath = Folder + "/CardGlow.png";
        private const string PillPath = Folder + "/Pill.png";
        private const string CirclePath = Folder + "/Circle.png";
        private const string BlockPath = Folder + "/Block.png";
        private const string GlovePath = Folder + "/Glove.png";

        /// <summary>The sprites the interface is built from.</summary>
        public readonly struct Sprites
        {
            public readonly Sprite Card;
            public readonly Sprite CardGlow;
            public readonly Sprite Pill;
            public readonly Sprite Circle;

            /// <summary>A plain rectangle. Needed wherever an Image is set to Filled, which will not draw without a sprite.</summary>
            public readonly Sprite Block;

            /// <summary>
            /// A right-facing boxing glove, for the fighter badges on the HUD.
            /// The opponent's is the same sprite mirrored, so the two gloves face
            /// each other across the top of the fight.
            /// </summary>
            public readonly Sprite Glove;

            public Sprites(Sprite card, Sprite cardGlow, Sprite pill, Sprite circle, Sprite block, Sprite glove)
            {
                Card = card;
                CardGlow = cardGlow;
                Pill = pill;
                Circle = circle;
                Block = block;
                Glove = glove;
            }

            public bool IsComplete =>
                Card != null && CardGlow != null && Pill != null && Circle != null && Block != null && Glove != null;
        }

        /// <summary>
        /// Regenerates every sprite and returns them. Existing files are
        /// overwritten, so this is the one place their look is decided.
        /// </summary>
        public static Sprites BuildAll()
        {
            EnsureFolder();

            // White with a hairline border. Tinted at runtime, so the fill is left
            // at full white here -- tinting can only darken, never brighten.
            Write(CardPath, RoundedRect(128, 128, 30f, 3f, Color.white, new Color32(0xD5, 0xDD, 0xE4, 0xFF)), 40f);

            Write(CardGlowPath, Halo(192, 192, 34f, 26f), 64f);

            // Radius is half the height, so the ends are semicircles at any width.
            Write(PillPath, RoundedRect(64, 64, 32f, 0f, Color.white, Color.clear), 32f);

            Write(CirclePath, RoundedRect(96, 96, 48f, 3f, Color.white, new Color32(0xD5, 0xDD, 0xE4, 0xFF)), 0f);

            // No rounding and no border: a square of solid white, for bars that
            // are clipped by fillAmount rather than resized.
            Write(BlockPath, RoundedRect(16, 16, 0f, 0f, Color.white, Color.clear), 0f);

            // A silhouette, so it can be tinted to whatever reads on the disc
            // behind it. No border and no rounding to preserve: drawn Simple.
            Write(GlovePath, Glove(128), 0f);

            AssetDatabase.Refresh();

            return new Sprites(
                AssetDatabase.LoadAssetAtPath<Sprite>(CardPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(CardGlowPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(PillPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath),
                AssetDatabase.LoadAssetAtPath<Sprite>(BlockPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(GlovePath));
        }

        /// <summary>
        /// Returns the sprites without redrawing them, generating them only if
        /// they are missing. For callers that want to use the shapes rather than
        /// decide what they look like.
        /// </summary>
        public static Sprites Load()
        {
            var loaded = new Sprites(
                AssetDatabase.LoadAssetAtPath<Sprite>(CardPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(CardGlowPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(PillPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath),
                AssetDatabase.LoadAssetAtPath<Sprite>(BlockPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(GlovePath));

            if (loaded.IsComplete)
            {
                return loaded;
            }

            return BuildAll();
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/UI"))
            {
                AssetDatabase.CreateFolder("Assets", "UI");
            }

            AssetDatabase.CreateFolder("Assets/UI", "Generated");
        }

        /// <summary>
        /// Writes the texture and sets it up as a nine-sliced sprite. The border
        /// has to be at least the corner radius, or stretching a card pulls its
        /// rounded corners out of shape -- which is the single most common way
        /// this style goes wrong.
        /// </summary>
        private static void Write(string path, Texture2D texture, float border)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"No texture importer for {path}; the sprite will not be usable.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            importer.SaveAndReimport();
        }

        /// <summary>
        /// A rounded rectangle, optionally with a border drawn inside its edge.
        /// A radius of half the shorter side gives a pill; half of a square gives
        /// a circle, which is why one function covers all four shapes.
        /// </summary>
        private static Texture2D RoundedRect(int width, int height, float radius, float borderWidth, Color fill, Color border)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            var half = new Vector2(width * 0.5f, height * 0.5f);
            radius = Mathf.Min(radius, Mathf.Min(half.x, half.y));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Sampled at the pixel centre, else the shape sits half a
                    // pixel off and the two sides anti-alias differently.
                    float distance = SignedDistance(x + 0.5f, y + 0.5f, half, radius);

                    // One pixel of coverage either side of the edge.
                    float inside = Mathf.Clamp01(0.5f - distance);

                    Color colour = fill;
                    if (borderWidth > 0f)
                    {
                        // The border occupies the band just inside the edge.
                        float borderMask = Mathf.Clamp01(distance + borderWidth + 0.5f);
                        colour = Color.Lerp(fill, border, borderMask);
                    }

                    colour.a *= inside;
                    pixels[(y * width) + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// A soft halo: solid inside the shape, falling away over
        /// <paramref name="feather"/> pixels outside it. Tinted and faded at
        /// runtime to light a card up.
        ///
        /// The falloff is squared rather than linear, which is what makes it read
        /// as light rather than as a grey outline.
        /// </summary>
        private static Texture2D Halo(int width, int height, float radius, float feather)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            var half = new Vector2(width * 0.5f, height * 0.5f);
            float inset = feather;
            var shapeHalf = new Vector2(half.x - inset, half.y - inset);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = SignedDistance(x + 0.5f, y + 0.5f, shapeHalf, radius, half);

                    float alpha = 1f;
                    if (distance > 0f)
                    {
                        float t = Mathf.Clamp01(1f - (distance / feather));
                        alpha = t * t;
                    }

                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// A boxing glove facing right, built the same way as everything else
        /// here: rounded boxes combined by signed distance rather than pixels
        /// pushed around.
        ///
        /// Three shapes make the silhouette -- the knuckle mass, the thumb and
        /// the cuff -- unioned by taking the nearest of them, and then a thin
        /// slot is cut where the cuff meets the fist so the glove still reads as
        /// a glove at badge size instead of as a blob.
        /// </summary>
        private static Texture2D Glove(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            // Authored against a 128 grid and scaled, so the proportions survive
            // a change of resolution.
            float s = size / 128f;

            Vector2 P(float x, float y) => new Vector2(x * s, y * s);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float fist = SignedDistance(px, py, P(32f, 30f), 28f * s, P(68f, 78f));
                    float thumb = SignedDistance(px, py, P(12f, 13f), 12f * s, P(30f, 64f));
                    float cuff = SignedDistance(px, py, P(26f, 14f), 10f * s, P(64f, 30f));

                    float shape = Mathf.Min(fist, Mathf.Min(thumb, cuff));
                    float alpha = Mathf.Clamp01(0.5f - shape);

                    // The wrist slot, cut back out of the union.
                    float slot = SignedDistance(px, py, P(30f, 2.5f), 2.5f * s, P(64f, 46f));
                    alpha *= Mathf.Clamp01(0.5f + slot);

                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float SignedDistance(float x, float y, Vector2 half, float radius)
        {
            return SignedDistance(x, y, half, radius, half);
        }

        /// <summary>
        /// Distance from a point to the edge of a rounded rectangle: negative
        /// inside, zero on the edge, positive outside. The rectangle is centred on
        /// <paramref name="centre"/> and extends <paramref name="half"/> either
        /// way.
        /// </summary>
        private static float SignedDistance(float x, float y, Vector2 half, float radius, Vector2 centre)
        {
            float dx = Mathf.Abs(x - centre.x) - (half.x - radius);
            float dy = Mathf.Abs(y - centre.y) - (half.y - radius);

            float outsideX = Mathf.Max(dx, 0f);
            float outsideY = Mathf.Max(dy, 0f);
            float outside = Mathf.Sqrt((outsideX * outsideX) + (outsideY * outsideY));

            // Inside the corner box, the nearest edge is whichever axis is closer.
            float insideDistance = Mathf.Min(Mathf.Max(dx, dy), 0f);

            return outside + insideDistance - radius;
        }
    }
}
