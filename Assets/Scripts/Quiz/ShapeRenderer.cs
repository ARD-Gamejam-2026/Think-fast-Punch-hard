using System.Collections.Generic;
using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Draws ShapeSpec values into sprites: one shape per call, or a horizontal
    /// strip of sequence terms ending in an outlined placeholder box.
    /// </summary>
    public sealed class ShapeRenderer
    {
        private const int CellSize = 128;
        private const float Margin = 0.14f;
        private const float OutlineBand = 0.10f;

        private static readonly Color[] Palette =
        {
            new Color(0.15f, 0.55f, 0.85f),
            new Color(0.95f, 0.60f, 0.10f),
            new Color(0.20f, 0.65f, 0.35f),
            new Color(0.85f, 0.30f, 0.55f),
        };

        /// <summary>Number of colors shapes can use; matches the generator's palette.</summary>
        public int PaletteSize
        {
            get { return Palette.Length; }
        }

        /// <summary>Renders a single shape spec to its own square sprite.</summary>
        public Sprite Render(ShapeSpec spec)
        {
            var pixels = NewTransparentBuffer(CellSize, CellSize);
            DrawCell(pixels, CellSize, CellSize, 0, 0, spec);
            return BuildSprite(pixels, CellSize, CellSize);
        }

        /// <summary>
        /// Renders the terms across two rows with the "?" placeholder centered on
        /// a third row below, keeping the prompt compact instead of one wide row.
        /// </summary>
        public Sprite RenderStrip(IReadOnlyList<ShapeSpec> terms)
        {
            int termCount = terms.Count;
            int columns = Mathf.Max(1, Mathf.CeilToInt(termCount / 2f));
            int width = columns * CellSize;
            int height = 3 * CellSize;
            var pixels = NewTransparentBuffer(width, height);
            for (int i = 0; i < termCount; i++)
            {
                int rowFromTop = i / columns;
                int column = i % columns;
                int inThisRow = Mathf.Min(columns, termCount - rowFromTop * columns);
                int rowLeft = (width - inThisRow * CellSize) / 2;
                int cellX = rowLeft + column * CellSize;
                int cellY = (2 - rowFromTop) * CellSize;
                DrawCell(pixels, width, height, cellX, cellY, terms[i]);
            }
            DrawPlaceholder(pixels, width, height, (width - CellSize) / 2, 0);
            return BuildSprite(pixels, width, height);
        }

        private Color[] NewTransparentBuffer(int width, int height)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            return pixels;
        }

        private void DrawCell(Color[] pixels, int texWidth, int texHeight, int cellX, int cellY, ShapeSpec spec)
        {
            Color color = Palette[Mathf.Clamp(spec.ColorIndex, 0, Palette.Length - 1)];
            int grid = Mathf.CeilToInt(Mathf.Sqrt(spec.Count));
            float sub = (float)CellSize / grid;
            int drawn = 0;
            for (int gy = 0; gy < grid && drawn < spec.Count; gy++)
            {
                for (int gx = 0; gx < grid && drawn < spec.Count; gx++)
                {
                    var rect = new Rect(cellX + gx * sub, cellY + gy * sub, sub, sub);
                    DrawShapeInRect(pixels, texWidth, texHeight, rect, spec, color);
                    drawn++;
                }
            }
        }

        private void DrawShapeInRect(Color[] pixels, int texWidth, int texHeight, Rect rect, ShapeSpec spec, Color color)
        {
            float inset = rect.width * Margin;
            var inner = new Rect(rect.x + inset, rect.y + inset,
                rect.width - inset * 2, rect.height - inset * 2);
            Vector2 center = inner.center;
            float radius = inner.width * 0.5f;
            Vector2[] polygon = BuildPolygon(spec.Kind, center, radius, spec.RotationDegrees);

            int x0 = Mathf.Max(0, Mathf.FloorToInt(rect.x));
            int x1 = Mathf.Min(texWidth - 1, Mathf.CeilToInt(rect.xMax));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(rect.y));
            int y1 = Mathf.Min(texHeight - 1, Mathf.CeilToInt(rect.yMax));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float coverage = Coverage(x, y, spec, center, radius, polygon);
                    if (coverage > 0f)
                    {
                        BlendPixel(pixels, texWidth, x, y, color, coverage);
                    }
                }
            }
        }

        private float Coverage(int x, int y, ShapeSpec spec, Vector2 center, float radius, Vector2[] polygon)
        {
            int inside = 0;
            for (int sy = 0; sy < 2; sy++)
            {
                for (int sx = 0; sx < 2; sx++)
                {
                    var p = new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f);
                    if (IsInsideShape(p, spec, center, radius, polygon))
                    {
                        inside++;
                    }
                }
            }
            return inside / 4f;
        }

        private bool IsInsideShape(Vector2 p, ShapeSpec spec, Vector2 center, float radius, Vector2[] polygon)
        {
            bool solid;
            float edgeDistance;
            if (spec.Kind == ShapeKind.Circle)
            {
                float d = Vector2.Distance(p, center);
                solid = d <= radius;
                edgeDistance = Mathf.Abs(d - radius);
            }
            else
            {
                solid = PointInPolygon(p, polygon);
                edgeDistance = DistanceToPolygon(p, polygon);
            }
            if (spec.Filled)
            {
                return solid;
            }
            return solid && edgeDistance <= radius * OutlineBand;
        }

        // --- geometry helpers ---

        private Vector2[] BuildPolygon(ShapeKind kind, Vector2 center, float radius, int rotationDegrees)
        {
            switch (kind)
            {
                case ShapeKind.Square:
                    return RegularPolygon(center, radius, 4, rotationDegrees + 45);
                case ShapeKind.Triangle:
                    return RegularPolygon(center, radius, 3, rotationDegrees - 90);
                case ShapeKind.Star:
                    return StarPolygon(center, radius, rotationDegrees - 90);
                default:
                    return RegularPolygon(center, radius, 24, rotationDegrees);
            }
        }

        private Vector2[] RegularPolygon(Vector2 center, float radius, int sides, int rotationDegrees)
        {
            var points = new Vector2[sides];
            float baseAngle = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                float a = baseAngle + i * (2f * Mathf.PI / sides);
                points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return points;
        }

        private Vector2[] StarPolygon(Vector2 center, float radius, int rotationDegrees)
        {
            var points = new Vector2[10];
            float baseAngle = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < 10; i++)
            {
                float r = radius;
                if (i % 2 == 1)
                {
                    r = radius * 0.45f;
                }
                float a = baseAngle + i * (Mathf.PI / 5f);
                points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return points;
        }

        private bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; i++)
            {
                bool crosses = (poly[i].y > p.y) != (poly[j].y > p.y);
                if (crosses)
                {
                    float t = (p.y - poly[i].y) / (poly[j].y - poly[i].y);
                    float xCross = poly[i].x + t * (poly[j].x - poly[i].x);
                    if (p.x < xCross)
                    {
                        inside = !inside;
                    }
                }
                j = i;
            }
            return inside;
        }

        private float DistanceToPolygon(Vector2 p, Vector2[] poly)
        {
            float best = float.MaxValue;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; i++)
            {
                float d = DistanceToSegment(p, poly[j], poly[i]);
                if (d < best)
                {
                    best = d;
                }
                j = i;
            }
            return best;
        }

        private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared < 1e-5f)
            {
                return Vector2.Distance(p, a);
            }
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
            return Vector2.Distance(p, a + ab * t);
        }

        private void DrawPlaceholder(Color[] pixels, int texWidth, int texHeight, int cellX, int cellY)
        {
            var spec = new ShapeSpec(ShapeKind.Square, 0, 1, 0, false);
            DrawShapeInRect(pixels, texWidth, texHeight,
                new Rect(cellX, cellY, CellSize, CellSize), spec, new Color(0.7f, 0.7f, 0.7f));
        }

        private void BlendPixel(Color[] pixels, int texWidth, int x, int y, Color color, float coverage)
        {
            int index = y * texWidth + x;
            Color existing = pixels[index];
            var incoming = new Color(color.r, color.g, color.b, coverage);
            float outA = incoming.a + existing.a * (1f - incoming.a);
            if (outA <= 0f)
            {
                pixels[index] = Color.clear;
                return;
            }
            Color rgb = (incoming * incoming.a + existing * existing.a * (1f - incoming.a)) / outA;
            pixels[index] = new Color(rgb.r, rgb.g, rgb.b, outA);
        }

        private Sprite BuildSprite(Color[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
