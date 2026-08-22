using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Curated set of Wikipedia topics for image-guessing questions (places,
    /// animals, ...). Titles are en.wikipedia.org page titles (e.g.
    /// "Eiffel_Tower" or "Red_panda").
    /// </summary>
    [CreateAssetMenu(menuName = "Quiz/Wikipedia Topic List", fileName = "Topics")]
    public class WikipediaTopicList : ScriptableObject
    {
        /// <summary>One topic: its Wikipedia page title and the label shown as an answer.</summary>
        [System.Serializable]
        public class Entry
        {
            public string wikipediaTitle;
            public string displayName;
        }

        public Entry[] entries = new Entry[0];
    }
}
