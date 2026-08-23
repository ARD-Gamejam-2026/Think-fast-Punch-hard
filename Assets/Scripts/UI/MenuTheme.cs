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

        /// <summary>
        /// The empty part of any bar -- health, Flow, the quiz timer. Light
        /// enough to read as "not filled yet" rather than as a second colour.
        /// </summary>
        public static readonly Color Track = new Color32(0xE4, 0xEA, 0xF0, 0xFF);

        /// <summary>Health, and a correct answer. Dark enough to hold its own against white.</summary>
        public static readonly Color Positive = new Color32(0x35, 0xB3, 0x6C, 0xFF);

        /// <summary>Low health, a wrong answer, and the last second on the clock.</summary>
        public static readonly Color Negative = new Color32(0xE0, 0x50, 0x3F, 0xFF);

        /// <summary>The middle of the timer, and the revealed right answer.</summary>
        public static readonly Color Caution = new Color32(0xF0, 0xA5, 0x2B, 0xFF);

        /// <summary>Flow state: the one moment the interface is allowed to shout.</summary>
        public static readonly Color Flow = new Color32(0xF5, 0xB9, 0x3C, 0xFF);

        /// <summary>
        /// Feedback fills for the answer buttons.
        ///
        /// Tints rather than solid colour, and that is a contrast decision, not a
        /// stylistic one: the button's label colour is fixed at build time and
        /// only its background changes state, so a saturated fill would leave dark
        /// grey type on a strong green or red. A tint keeps the label at better
        /// than 8:1 against every state while still reading instantly as
        /// right or wrong.
        /// </summary>
        public static readonly Color PositiveTint = new Color32(0xC2, 0xEC, 0xD1, 0xFF);

        /// <summary>Wrong answer fill. See <see cref="PositiveTint"/> for why it is a tint.</summary>
        public static readonly Color NegativeTint = new Color32(0xFA, 0xCB, 0xC6, 0xFF);

        /// <summary>The right answer, revealed after a wrong pick or a timeout.</summary>
        public static readonly Color CautionTint = new Color32(0xFD, 0xE5, 0xB4, 0xFF);

        /// <summary>
        /// Backing for UI drawn over the fight. The HUD sits on this rather than
        /// straight on the stage, so its bars keep their contrast whatever colour
        /// the level behind them turns out to be.
        /// </summary>
        public static readonly Color HudCard = new Color32(0xFF, 0xFF, 0xFF, 0xF0);

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
