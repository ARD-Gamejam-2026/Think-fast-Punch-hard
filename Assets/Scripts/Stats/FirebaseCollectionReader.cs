using System.Collections.Generic;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Splits a Firebase Realtime Database collection response (an object keyed
    /// by push ids) into its member value objects. Pure string handling with no
    /// UnityEngine dependency, so it can be unit-tested without the editor.
    /// </summary>
    internal static class FirebaseCollectionReader
    {
        /// <summary>
        /// Yields each member's value object (the <c>{...}</c> after a push id),
        /// tracking brace depth. Braces inside quoted strings — e.g. a player
        /// name containing <c>{</c> or <c>}</c> — and escaped quotes are ignored,
        /// so a name is never mistaken for structure.
        /// </summary>
        public static IEnumerable<string> SplitValueObjects(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                yield break;
            }

            int depth = 0;
            int valueStart = -1;
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{')
                {
                    depth++;
                    if (depth == 2)
                    {
                        valueStart = i;
                    }
                }
                else if (c == '}')
                {
                    if (depth == 2 && valueStart >= 0)
                    {
                        yield return json.Substring(valueStart, i - valueStart + 1);
                        valueStart = -1;
                    }

                    depth--;
                }
            }
        }
    }
}
