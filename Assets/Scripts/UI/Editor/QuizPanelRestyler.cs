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

        /// <summary>The per-answer shape image, as named by the quiz's own sequence setup.</summary>
        private const string AnswerIconName = "AnswerIcon";

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

            Paint(bar.GetComponent<Image>(), sprites.Block, MenuTheme.Track);

            Transform fill = bar.Find("TimerFill");
            if (fill == null)
            {
                return;
            }

            var fillImage = fill.GetComponent<Image>();
            if (fillImage == null)
            {
                return;
            }

            // Filled, and it MUST stay Filled. The timer empties by setting
            // fillAmount, which only does anything on this image type -- an
            // earlier pass set it to Sliced and the bar stopped draining, leaving
            // a timer that changed colour while staying full. Filled also needs a
            // sprite: with none it draws nothing at all.
            fillImage.sprite = sprites.Block;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            // The colour is the timer's own business: QuizView repaints this every
            // frame to show which speed zone the answer is currently in.
        }

        private static void RestyleAnswers(GameObject root, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            foreach (AnswerButton answer in root.GetComponentsInChildren<AnswerButton>(includeInactive: true))
            {
                // Grey chips on a white panel, not white on white. White buttons on
                // a white card left only a hairline separating them, which is why
                // the panel read as flat: sitting a shade below the panel gives
                // each answer an edge without adding a single line.
                //
                // The slice scale is a separate matter -- the card's corner radius
                // was drawn for a tile several hundred pixels tall, and an answer
                // button is 72, where that radius exceeds half the height and the
                // nine-slice corners would meet in the middle and distort.
                Paint(answer.GetComponent<Image>(), sprites.Card, MenuTheme.Background, sliceScale: 1.8f);

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
                ConfigureAnswerButton(answer);
            }
        }

        /// <summary>
        /// Sets the four feedback fills and re-points the shape icon.
        ///
        /// Tints rather than solid colours for the fills, because the label's
        /// colour is fixed and only the background changes -- see
        /// <see cref="MenuTheme.PositiveTint"/>.
        ///
        /// The icon reference is restored rather than merely left alone: saving
        /// the prefab back drops it, and a null icon is not a cosmetic problem.
        /// A sequence question calls SetAnswerImage on every answer, and that
        /// method does not check for null, so the whole quiz throws the moment a
        /// shape question comes up.
        /// </summary>
        private static void ConfigureAnswerButton(AnswerButton answer)
        {
            var so = new SerializedObject(answer);

            // Must match the chip colour painted above: this is what the button is
            // reset to at the start of every question, so a mismatch here would
            // repaint all four white again on question two.
            so.FindProperty("normalColor").colorValue = MenuTheme.Background;
            so.FindProperty("correctColor").colorValue = MenuTheme.PositiveTint;
            so.FindProperty("wrongColor").colorValue = MenuTheme.NegativeTint;
            so.FindProperty("highlightColor").colorValue = MenuTheme.CautionTint;

            RestoreAnswerIcon(answer, so);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Points the button's icon field back at the child that draws it, found
        /// by name the same way the quiz's own sequence setup creates it.
        /// </summary>
        private static void RestoreAnswerIcon(AnswerButton answer, SerializedObject so)
        {
            SerializedProperty property = so.FindProperty("answerIcon");
            if (property == null)
            {
                return;
            }

            Transform icon = answer.transform.Find(AnswerIconName);
            if (icon == null)
            {
                return;
            }

            var image = icon.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            property.objectReferenceValue = image;
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

        /// <summary>
        /// Applies colour, font and weight to one label.
        ///
        /// Weight is not decoration here. Nunito is a variable font and Google
        /// Fonts ships only its default instance, which TMP builds at Regular --
        /// a light, round face that goes weak against a white panel however dark
        /// the ink is. Bold is what makes the type hold the page.
        /// </summary>
        private static void SetText(Transform target, Color colour, TMP_FontAsset font, FontStyles style = FontStyles.Bold)
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
            label.fontStyle = style;

            if (font != null)
            {
                label.font = font;
            }
        }
    }
}
