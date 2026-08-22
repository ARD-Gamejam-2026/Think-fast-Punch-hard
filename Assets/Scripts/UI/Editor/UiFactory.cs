using ThinkFast.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// The small vocabulary the menu screens are written in: rects, images,
    /// labels, cards, round buttons and sliders.
    ///
    /// Kept apart from the builder so the builder reads as layout rather than as
    /// a wall of RectTransform arithmetic. Everything here is deliberately
    /// unopinionated about *where* things go -- that is the builder's job.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>Creates an empty UI object under a parent.</summary>
        public static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            return rect;
        }

        /// <summary>Pins a rect to fill its parent, optionally inset on every side.</summary>
        public static RectTransform Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>
        /// Places a rect at a normalized anchor point with a fixed size, which is
        /// how everything that is not stretched is positioned here.
        /// </summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Adds a sliced image to a rect. A null sprite gives a plain rectangle.</summary>
        public static Image AddImage(RectTransform rect, Sprite sprite, Color colour, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = raycast;

            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        /// <summary>Adds a label under a parent. Size is in the canvas's reference pixels.</summary>
        public static TMP_Text AddLabel(
            RectTransform parent,
            string name,
            string text,
            float size,
            Color colour,
            TMP_FontAsset font,
            FontStyles style = FontStyles.Normal,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();

            label.text = text;
            label.fontSize = size;
            label.color = colour;
            label.fontStyle = style;
            label.alignment = alignment;
            label.raycastTarget = false;

            if (font != null)
            {
                label.font = font;
            }

            return label;
        }

        /// <summary>
        /// Builds one card-shaped button: halo behind, card in front, an empty
        /// preview panel, and the label along the bottom.
        ///
        /// The halo is a sibling behind the card rather than a child of it,
        /// because a child would be scaled by the card's hover growth and the glow
        /// would visibly pump along with it.
        /// </summary>
        public static Button MakeCard(
            RectTransform parent,
            string name,
            string text,
            Vector2 size,
            UiSpriteFactory.Sprites sprites,
            TMP_FontAsset font,
            float labelSize,
            out RectTransform preview)
        {
            RectTransform root = NewRect(name, parent);
            root.sizeDelta = size;

            RectTransform glowRect = Stretch(NewRect("Glow", root), -18f);
            Image glow = AddImage(glowRect, sprites.CardGlow, new Color(0f, 0f, 0f, 0f));

            RectTransform cardRect = Stretch(NewRect("Card", root));
            Image card = AddImage(cardRect, sprites.Card, MenuTheme.Surface, raycast: true);

            // The inset panel a channel tile shows its preview in. Left flat and
            // empty: it is the obvious slot for art later, and until then it gives
            // the tile a face instead of leaving it a blank rectangle.
            preview = Place(
                NewRect("Preview", cardRect),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -18f),
                new Vector2(size.x - 36f, size.y - 96f));
            AddImage(preview, sprites.Card, MenuTheme.Background);

            TMP_Text label = AddLabel(cardRect, "Label", text, labelSize, MenuTheme.TextPrimary, font, FontStyles.Bold);
            Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(size.x - 32f, 58f));

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = card;

            WireMenuButton(root.gameObject.AddComponent<MenuButton>(), card, glow, label);
            return button;
        }

        /// <summary>Builds a small circular button, for closing a panel.</summary>
        public static Button MakeRound(
            RectTransform parent,
            string name,
            string text,
            float diameter,
            UiSpriteFactory.Sprites sprites,
            TMP_FontAsset font)
        {
            RectTransform root = NewRect(name, parent);
            root.sizeDelta = new Vector2(diameter, diameter);

            RectTransform glowRect = Stretch(NewRect("Glow", root), -12f);
            Image glow = AddImage(glowRect, sprites.CardGlow, new Color(0f, 0f, 0f, 0f));

            RectTransform circleRect = Stretch(NewRect("Circle", root));
            Image circle = AddImage(circleRect, sprites.Circle, MenuTheme.Surface, raycast: true);

            // Simple, not sliced: a circle has no middle that can be stretched.
            circle.type = Image.Type.Simple;

            TMP_Text label = AddLabel(circleRect, "Label", text, diameter * 0.4f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            Stretch(label.rectTransform);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = circle;

            WireMenuButton(root.gameObject.AddComponent<MenuButton>(), circle, glow, label);
            return button;
        }

        /// <summary>
        /// Builds a captioned slider: pill track, accent fill, round handle. The
        /// caption sits to the left of the bar rather than above it, so a row of
        /// them stays one line tall.
        /// </summary>
        public static Slider MakeSlider(
            RectTransform parent,
            string name,
            string caption,
            Vector2 size,
            float captionWidth,
            UiSpriteFactory.Sprites sprites,
            TMP_FontAsset font)
        {
            RectTransform root = NewRect(name, parent);
            root.sizeDelta = size;

            TMP_Text label = AddLabel(root, "Caption", caption, 28f, MenuTheme.TextMuted, font, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(captionWidth, size.y));

            RectTransform barRect = Place(
                NewRect("Bar", root),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(captionWidth + 20f, 0f),
                new Vector2(size.x - captionWidth - 20f, size.y));

            return BuildSliderParts(barRect, sprites);
        }

        /// <summary>
        /// Assembles the four rects Unity's Slider drives: track, fill area, fill
        /// and handle. The fill area and the handle area are both inset by the
        /// handle's radius, which is what stops the handle hanging over the ends
        /// of the track at 0 and 1.
        /// </summary>
        private static Slider BuildSliderParts(RectTransform barRect, UiSpriteFactory.Sprites sprites)
        {
            const float TrackHeight = 14f;
            const float HandleDiameter = 34f;
            float inset = HandleDiameter * 0.5f;

            RectTransform track = NewRect("Track", barRect);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.offsetMin = new Vector2(0f, -TrackHeight * 0.5f);
            track.offsetMax = new Vector2(0f, TrackHeight * 0.5f);
            AddImage(track, sprites.Pill, new Color32(0xDF, 0xE6, 0xEC, 0xFF), raycast: true);

            RectTransform fillArea = NewRect("Fill Area", barRect);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(inset, -TrackHeight * 0.5f);
            fillArea.offsetMax = new Vector2(-inset, TrackHeight * 0.5f);

            RectTransform fill = NewRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(-inset, 0f);
            fill.offsetMax = new Vector2(inset, 0f);
            AddImage(fill, sprites.Pill, MenuTheme.Accent);

            RectTransform handleArea = NewRect("Handle Slide Area", barRect);
            handleArea.anchorMin = new Vector2(0f, 0f);
            handleArea.anchorMax = new Vector2(1f, 1f);
            handleArea.offsetMin = new Vector2(inset, 0f);
            handleArea.offsetMax = new Vector2(-inset, 0f);

            RectTransform handle = NewRect("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(HandleDiameter, HandleDiameter);
            Image handleImage = AddImage(handle, sprites.Circle, MenuTheme.Surface, raycast: true);
            handleImage.type = Image.Type.Simple;

            var slider = barRect.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static void WireMenuButton(MenuButton menuButton, Image background, Image glow, TMP_Text label)
        {
            var so = new SerializedObject(menuButton);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("glow").objectReferenceValue = glow;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
