namespace ThinkFast.Quiz
{
    /// <summary>
    /// A generated shape sequence: the shown shape terms, four answer shapes,
    /// and the index of the correct next shape among the options.
    /// </summary>
    public sealed class ShapePuzzle
    {
        /// <summary>The shape terms shown to the player (correct shape excluded).</summary>
        public ShapeSpec[] Terms { get; set; }

        /// <summary>The four answer options, one of which is the correct next shape.</summary>
        public ShapeSpec[] Options { get; set; }

        /// <summary>Index of the correct next shape within Options.</summary>
        public int CorrectIndex { get; set; }
    }
}
