using UnityEngine;

namespace ThinkFast.UI
{
    /// <summary>
    /// The menu's palette and timings, in one place.
    ///
    /// Shared by the generator that builds the screens and the components that
    /// animate them, because the two have to agree: a hover colour picked in the
    /// builder and a hover colour picked in the button would drift apart the
    /// first time either was touched.
    ///
    /// The look is soft-console UI -- near-white ground, white cards, one blue
    /// accent, dark grey type rather than black, and generous rounding. Black on
    /// white is the thing that makes an interface feel severe, so nothing here is
    /// fully black or fully saturated.
    /// </summary>
    public static class MenuTheme
    {
        /// <summary>The page behind everything. Very slightly grey, so white cards read as raised.</summary>
        public static readonly Color Background = new Color32(0xF1, 0xF4, 0xF7, 0xFF);

        /// <summary>Cards, tiles and bars.</summary>
        public static readonly Color Surface = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        /// <summary>The hairline around a resting card.</summary>
        public static readonly Color Border = new Color32(0xD5, 0xDD, 0xE4, 0xFF);

        /// <summary>The one accent. Used for hover, focus and fills, and nothing else.</summary>
        public static readonly Color Accent = new Color32(0x14, 0xA5, 0xDC, 0xFF);

        /// <summary>Accent at card-fill strength, for a hovered tile's wash.</summary>
        public static readonly Color AccentWash = new Color32(0xEA, 0xF7, 0xFD, 0xFF);

        /// <summary>Body and label type. Dark grey, never black.</summary>
        public static readonly Color TextPrimary = new Color32(0x3C, 0x46, 0x50, 0xFF);

        /// <summary>Secondary type: captions, slider labels.</summary>
        public static readonly Color TextMuted = new Color32(0x8A, 0x97, 0xA3, 0xFF);

        /// <summary>Type on an accent-filled surface.</summary>
        public static readonly Color TextOnAccent = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        /// <summary>The wash a screen fades in from and out to.</summary>
        public static readonly Color FadeSheet = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        /// <summary>Scale a tile grows to when the pointer is over it.</summary>
        public const float HoverScale = 1.04f;

        /// <summary>Scale a tile dips to while held down. Smaller than resting, so the press is felt.</summary>
        public const float PressScale = 0.975f;

        /// <summary>
        /// Seconds for a hover or press to reach its target. Short enough to feel
        /// immediate, long enough that the eye reads it as movement rather than a
        /// jump.
        /// </summary>
        public const float StateDuration = 0.11f;

        /// <summary>Seconds a screen takes to fade in once it is loaded.</summary>
        public const float FadeInDuration = 0.28f;

        /// <summary>Seconds a screen takes to wash out before the next one loads.</summary>
        public const float FadeOutDuration = 0.22f;
    }
}
