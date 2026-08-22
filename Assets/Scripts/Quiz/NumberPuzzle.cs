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

        /// <summary>The four answer options, one of which is the correct next term.</summary>
        public string[] Options { get; set; }

        /// <summary>Index of the correct next term within Options.</summary>
        public int CorrectIndex { get; set; }
    }
}
