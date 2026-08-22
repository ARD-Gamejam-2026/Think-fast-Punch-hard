using System;

namespace ThinkFast.Quiz
{
    /// <summary>The kind of shape drawn for a sequence term.</summary>
    public enum ShapeKind
    {
        Circle,
        Square,
        Triangle,
        Star,
    }

    /// <summary>
    /// Immutable description of one drawn shape: its kind, rotation, how many
    /// copies to draw, its palette color index, and whether it is filled.
    /// </summary>
    public readonly struct ShapeSpec : IEquatable<ShapeSpec>
    {
        /// <summary>Which kind of shape to draw.</summary>
        public ShapeKind Kind { get; }

        /// <summary>Clockwise rotation in degrees, normalized to 0-359.</summary>
        public int RotationDegrees { get; }

        /// <summary>How many copies of the shape to draw (at least one).</summary>
        public int Count { get; }

        /// <summary>Index into the renderer's color palette.</summary>
        public int ColorIndex { get; }

        /// <summary>True when the shape is filled, false when outlined.</summary>
        public bool Filled { get; }

        /// <summary>Creates a shape spec, normalizing rotation and clamping count.</summary>
        public ShapeSpec(ShapeKind kind, int rotationDegrees, int count, int colorIndex, bool filled)
        {
            Kind = kind;
            RotationDegrees = ((rotationDegrees % 360) + 360) % 360;
            if (count < 1)
            {
                count = 1;
            }
            Count = count;
            ColorIndex = colorIndex;
            Filled = filled;
        }

        /// <summary>Returns true when every field equals the other spec's.</summary>
        public bool Equals(ShapeSpec other)
        {
            return Kind == other.Kind
                && RotationDegrees == other.RotationDegrees
                && Count == other.Count
                && ColorIndex == other.ColorIndex
                && Filled == other.Filled;
        }

        /// <summary>Returns true when the object is an equal shape spec.</summary>
        public override bool Equals(object obj)
        {
            if (obj is ShapeSpec other)
            {
                return Equals(other);
            }
            return false;
        }

        /// <summary>Returns a hash combining all shape fields.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, RotationDegrees, Count, ColorIndex, Filled);
        }

        /// <summary>Returns true when both shape specs are equal.</summary>
        public static bool operator ==(ShapeSpec left, ShapeSpec right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true when the shape specs differ.</summary>
        public static bool operator !=(ShapeSpec left, ShapeSpec right)
        {
            return !left.Equals(right);
        }
    }
}
