using ThinkFast.Quiz;
using ThinkFast.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Repaints the quiz panel prefab in the game's palette: white card, dark
    /// grey type, rounded shapes, one blue accent.
    ///
    /// A separate command rather than an edit to the quiz team's own generator,
    /// which is theirs to own -- and re-running theirs is documented as resetting
    /// the styling, so this has to be re-runnable too rather than a one-off pass
    /// by hand. Re-run it after **Tools > Quiz > Create Quiz Panel**.
    ///
    /// It changes colours, sprites and fonts. It does not touch layout, sizes or
    /// the hierarchy, so the quiz half keeps working exactly as it did.
    /// </summary>
    public static class QuizPanelRestyler
    {
        private const string PrefabPath = "Assets/Quiz/QuizPanel.prefab";
        private const string FontPath = "Assets/Fonts/Nunito SDF.asset";

        [MenuItem("Tools/Think Fast/Restyle Quiz Panel")]
        public static void Restyle()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"No quiz panel prefab at {PrefabPath}.");
                return;
            }

            try
            {
                UiSpriteFactory.Sprites sprites = UiSpriteFactory.Load();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

                RestyleContainer(root, sprites);
                RestyleTimer(root, sprites);
                RestyleAnswers(root, sprites, font);
                RestyleQuestion(root, font);
                RestyleTimerZones(root);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("Restyled the quiz panel. Re-run Tools > Think Fast > Build Split Screen Fight to put it back in the fight scene.");
            }
            finally
            {
                // Prefab contents are a temporary scene. Not unloading it leaks the
                // objects and leaves the prefab locked for the rest of the session.
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RestyleContainer(GameObject root, UiSpriteFactory.Sprites sprites)
        {
            Transform container = root.transform.Find("Container");
            if (container == null)
            {
                Debug.LogWarning("No Container in the quiz panel; its background was left as it was.");
                return;
            }

            Paint(container.GetComponent<Image>(), sprites.Card, MenuTheme.Surface);
        }

        private static void RestyleTimer(GameObject root, UiSpriteFactory.Sprites sprites)
        {
            Transform bar = root.transform.Find("Container/TimerBar");
            if (bar == null)
            {
                return;
            }

            Paint(bar.GetComponent<Image>(), sprites.Pill, MenuTheme.Track);

            Transform fill = bar.Find("TimerFill");
            if (fill == null)
            {
                return;
            }

            // Only the shape is set here. The colour is the timer's own business:
            // QuizView repaints this every frame to show which speed zone the
            // answer is currently in.
            var fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.sprite = sprites.Pill;
                fillImage.type = Image.Type.Sliced;
            }
        }

        private static void RestyleAnswers(GameObject root, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            foreach (AnswerButton answer in root.GetComponentsInChildren<AnswerButton>(includeInactive: true))
            {
                // The card's corner radius was drawn for a tile several hundred
                // pixels tall. An answer button is 72, where that radius is larger
                // than half the height -- the nine-slice corners would meet in the
                // middle and distort. The multiplier scales the slice down so the
                // rounding stays proportional instead.
                Paint(answer.GetComponent<Image>(), sprites.Card, MenuTheme.Surface, sliceScale: 1.8f);

                Transform letterBox = answer.transform.Find("LetterBox");
                if (letterBox != null)
                {
                    // A circle rather than a scaled-down card: the box is square
                    // and 56 px, which is too small for the card's rounding to
                    // survive at any slice, and a round badge reads better beside
                    // a rounded button anyway.
                    Image box = letterBox.GetComponent<Image>();
                    if (box != null)
                    {
                        box.sprite = sprites.Circle;
                        box.type = Image.Type.Simple;
                        box.color = MenuTheme.AccentWash;
                    }

                    SetText(letterBox.Find("LetterLabel"), MenuTheme.Accent, font);
                }

                SetText(answer.transform.Find("AnswerLabel"), MenuTheme.TextPrimary, font);
                SetStateColours(answer);
            }
        }

        /// <summary>
        /// Sets the four feedback fills. Tints rather than solid colours, because
        /// the label's colour is fixed and only the background changes -- see
        /// <see cref="MenuTheme.PositiveTint"/>.
        /// </summary>
        private static void SetStateColours(AnswerButton answer)
        {
            var so = new SerializedObject(answer);
            so.FindProperty("normalColor").colorValue = MenuTheme.Surface;
            so.FindProperty("correctColor").colorValue = MenuTheme.PositiveTint;
            so.FindProperty("wrongColor").colorValue = MenuTheme.NegativeTint;
            so.FindProperty("highlightColor").colorValue = MenuTheme.CautionTint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RestyleQuestion(GameObject root, TMP_FontAsset font)
        {
            SetText(root.transform.Find("Container/TopPane/QuestionLabel"), MenuTheme.TextPrimary, font);
        }

        /// <summary>
        /// Repoints the timer's three speed zones at the shared palette, so the
        /// green a player learns to aim for is the same green the rest of the game
        /// uses for something going right.
        /// </summary>
        private static void RestyleTimerZones(GameObject root)
        {
            var view = root.GetComponent<QuizView>();
            if (view == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("timerFastColor").colorValue = MenuTheme.Positive;
            so.FindProperty("timerMidColor").colorValue = MenuTheme.Caution;
            so.FindProperty("timerLastSecondColor").colorValue = MenuTheme.Negative;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Applies a shape and a colour. <paramref name="sliceScale"/> above 1
        /// shrinks the nine-slice border, which is how one card sprite serves
        /// surfaces of very different sizes without its corners distorting on the
        /// small ones.
        /// </summary>
        private static void Paint(Image image, Sprite sprite, Color colour, float sliceScale = 1f)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = colour;
            image.pixelsPerUnitMultiplier = sliceScale;
        }

        private static void SetText(Transform target, Color colour, TMP_FontAsset font)
        {
            if (target == null)
            {
                return;
            }

            var label = target.GetComponent<TMP_Text>();
            if (label == null)
            {
                return;
            }

            label.color = colour;

            if (font != null)
            {
                label.font = font;
            }
        }
    }
}
