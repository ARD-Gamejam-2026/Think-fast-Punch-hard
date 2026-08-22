using UnityEngine;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Curated set of landmarks for place-guessing questions. Titles are
    /// en.wikipedia.org page titles (e.g. "Eiffel_Tower").
    /// </summary>
    [CreateAssetMenu(menuName = "Quiz/Landmark List", fileName = "Landmarks")]
    public class LandmarkList : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string wikipediaTitle;
            public string displayName;
        }

        public Entry[] entries = new Entry[0];
    }
}
