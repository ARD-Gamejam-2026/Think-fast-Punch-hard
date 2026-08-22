using System;

namespace ThinkFast.Quiz
{
    /// <summary>Subset of the Wikipedia REST page-summary response.</summary>
    [Serializable]
    public class WikipediaSummary
    {
        /// <summary>The page's lead image, if any (field names match the JSON).</summary>
        [Serializable]
        public class Thumbnail
        {
            public string source;
            public int width;
            public int height;
        }

        public string title;
        public Thumbnail thumbnail;
    }
}
