namespace ThinkFast.Quiz
{
    /// <summary>
    /// A generated number sequence: the shown terms, four answer options, and
    /// the index of the correct next term among the options.
    /// </summary>
    public sealed class NumberPuzzle
    {
        /// <summary>The sequence terms shown to the player (correct term excluded).</summary>
        public int[] Terms { get; set; }

        /// <summary>The four answer strings, one of which is the correct next term.</summary>
        public string[] Options { get; set; }

        /// <summary>Index of the correct next term within Options.</summary>
        public int CorrectIndex { get; set; }
    }

    /// <summary>
    /// A generated shape sequence: the shown shape terms, four answer shapes,
    /// and the index of the correct next shape among the options.
    /// </summary>
    public sealed class ShapePuzzle
    {
        /// <summary>The shape terms shown to the player (correct shape excluded).</summary>
        public ShapeSpec[] Terms { get; set; }

        /// <summary>The four answer shapes, one of which is the correct next shape.</summary>
        public ShapeSpec[] Options { get; set; }

        /// <summary>Index of the correct next shape within Options.</summary>
        public int CorrectIndex { get; set; }
    }
}
