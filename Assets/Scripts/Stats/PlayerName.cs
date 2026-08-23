using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// The player's chosen name, entered on the menu and stored in PlayerPrefs so
    /// it survives scene loads and app restarts. Defaults to "anon".
    /// </summary>
    public static class PlayerName
    {
        private const string Key = "thinkfast.playerName";
        private const string Fallback = "anon";

        /// <summary>The stored player name, or "anon" if none is set.</summary>
        public static string Value
        {
            get
            {
                string stored = PlayerPrefs.GetString(Key, Fallback);
                if (string.IsNullOrWhiteSpace(stored))
                {
                    return Fallback;
                }

                return stored;
            }
        }

        /// <summary>Stores the player name, ignoring blank input.</summary>
        public static void Set(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                PlayerPrefs.SetString(Key, Fallback);
            }
            else
            {
                PlayerPrefs.SetString(Key, name.Trim());
            }

            PlayerPrefs.Save();
        }
    }
}
