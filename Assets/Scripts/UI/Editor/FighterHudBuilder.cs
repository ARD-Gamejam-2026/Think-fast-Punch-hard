using ThinkFast.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Builds the fighter HUD as real uGUI objects in the open scene.
    ///
    /// Generated rather than hand-authored for the same reason the test rig is:
    /// re-runnable, reviewable as code, and no scene YAML edited by hand. After
    /// generation it is ordinary UI -- move it, recolour it, resize it in the
    /// editor and the changes stick until you rebuild it.
    /// </summary>
    public static class FighterHudBuilder
    {
        private const string RootName = "--- Fighter HUD (generated) ---";

        private const float PanelWidth = 400f;
        private const float BarHeight = 24f;
        private const float PipSize = 22f;
        private const float Gap = 10f;

        /// <summary>Breathing room between the card's edge and the bars inside it.</summary>
        private const float CardPadding = 18f;

        [MenuItem("Tools/Think Fast/Build Fighter HUD")]
        public static void Build()
        {
            RemoveExisting();

            UiSpriteFactory.Sprites sprites = UiSpriteFactory.Load();

            GameObject root = CreateCanvas();
            RectTransform panel = CreatePanel(root.transform, sprites);

            // Laid out upward from the bottom: health is the thing you check most
            // often under pressure, so it sits closest to the fight.
            RectTransform apRow = CreateRow(panel, "Action Points", 0f, PipSize);
            Image[] pips = CreatePips(apRow, 5, sprites);

            RectTransform flowRow = CreateRow(panel, "Flow", PipSize + Gap, BarHeight);
            Image flowFill = CreateBar(flowRow, MenuTheme.Accent, sprites);

            RectTransform healthRow = CreateRow(panel, "Health", PipSize + Gap + BarHeight + Gap, BarHeight);
            Image healthFill = CreateBar(healthRow, MenuTheme.Positive, sprites);

            var hud = root.AddComponent<FighterHud>();
            Wire(hud, healthFill, flowFill, pips);

            Undo.RegisterCreatedObjectUndo(root, "Build Fighter HUD");
            Selection.activeGameObject = root;

            // Without this the new HUD is not part of the scene's unsaved state,
            // so it silently vanishes if the scene is reloaded without a save.
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("Built the fighter HUD. It finds the player's Health and FighterResources automatically on Play.");
        }

        private static void RemoveExisting()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go.name == RootName)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static GameObject CreateCanvas()
        {
            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above the quiz UI is wrong and below it is wrong; they occupy
            // different halves. Left at 0 so ordering stays predictable.
            canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Bias toward height: the fighter view keeps its vertical extent under
            // split screen but loses width, so scaling on width would shrink the
            // HUD as soon as the layout changes.
            scaler.matchWidthOrHeight = 1f;

            return root;
        }

        private static RectTransform CreatePanel(Transform parent, UiSpriteFactory.Sprites sprites)
        {
            var card = new GameObject("Card", typeof(RectTransform)).GetComponent<RectTransform>();
            card.SetParent(parent, worldPositionStays: false);

            // Bottom-left: the fighter's half of the screen.
            card.anchorMin = Vector2.zero;
            card.anchorMax = Vector2.zero;
            card.pivot = Vector2.zero;
            card.anchoredPosition = new Vector2(28f, 28f);
            card.sizeDelta = new Vector2(PanelWidth + (CardPadding * 2f), 120f + (CardPadding * 2f));

            // The bars sit on a card rather than straight on the stage. That is a
            // contrast decision before it is a stylistic one: a bar drawn directly
            // over the fight has to survive whatever colour happens to be behind
            // it that frame, and the stage is going to change. On a near-opaque
            // card, every reading holds no matter what the level looks like.
            var background = card.gameObject.AddComponent<Image>();
            background.sprite = sprites.Card;
            background.type = Image.Type.Sliced;
            background.color = MenuTheme.HudCard;
            background.raycastTarget = false;

            var panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(card, worldPositionStays: false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = new Vector2(CardPadding, CardPadding);
            panel.offsetMax = new Vector2(-CardPadding, -CardPadding);

            return panel;
        }

        private static RectTransform CreateRow(RectTransform parent, string name, float bottomOffset, float height)
        {
            var row = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, worldPositionStays: false);

            row.anchorMin = Vector2.zero;
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = Vector2.zero;
            row.anchoredPosition = new Vector2(0f, bottomOffset);
            row.sizeDelta = new Vector2(0f, height);

            return row;
        }

        /// <summary>
        /// A dark track with a coloured fill child. The fill is stretched by its
        /// anchors at runtime, which is why it needs no sprite.
        /// </summary>
        private static Image CreateBar(RectTransform row, Color fillColour, UiSpriteFactory.Sprites sprites)
        {
            Image track = CreateImage(row, "Track", MenuTheme.Track);
            track.sprite = sprites.Pill;
            track.type = Image.Type.Sliced;
            Stretch(track.rectTransform);

            Image fill = CreateImage(track.rectTransform, "Fill", fillColour);
            fill.sprite = sprites.Pill;
            fill.type = Image.Type.Sliced;

            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            // No inset. The fill is a rounded pill on a rounded track, so insetting
            // it would leave a sliver of track showing inside the fill's own
            // rounded end, which reads as the bar never quite reaching full.
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return fill;
        }

        private static Image[] CreatePips(RectTransform row, int count, UiSpriteFactory.Sprites sprites)
        {
            var pips = new Image[count];

            for (int i = 0; i < count; i++)
            {
                Image pip = CreateImage(row, $"Pip {i}", Color.white);
                pip.sprite = sprites.Circle;
                pip.type = Image.Type.Simple;

                RectTransform rect = pip.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(PipSize, PipSize);
                rect.anchoredPosition = new Vector2(i * (PipSize + 8f), 0f);

                pips[i] = pip;
            }

            return pips;
        }

        private static Image CreateImage(Transform parent, string name, Color colour)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();

            image.transform.SetParent(parent, worldPositionStays: false);
            image.color = colour;

            // No sprite: Unity draws a plain white quad, tinted by the colour.
            // That is all a placeholder bar needs, and it keeps the HUD free of
            // any art dependency.
            image.sprite = null;
            image.raycastTarget = false;

            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Wire(FighterHud hud, Image healthFill, Image flowFill, Image[] pips)
        {
            var so = new SerializedObject(hud);

            so.FindProperty("healthFill").objectReferenceValue = healthFill.rectTransform;
            so.FindProperty("healthFillImage").objectReferenceValue = healthFill;
            so.FindProperty("flowFill").objectReferenceValue = flowFill.rectTransform;
            so.FindProperty("flowFillImage").objectReferenceValue = flowFill;

            SerializedProperty pipArray = so.FindProperty("actionPointPips");
            pipArray.arraySize = pips.Length;
            for (int i = 0; i < pips.Length; i++)
            {
                pipArray.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
            }

            // The colours have to be written here, not just onto the images. The
            // HUD re-applies them every frame from its own fields, so a bar tinted
            // at build time would be repainted with the old palette on frame one.
            so.FindProperty("healthColour").colorValue = MenuTheme.Positive;
            so.FindProperty("healthLowColour").colorValue = MenuTheme.Negative;
            so.FindProperty("flowColour").colorValue = MenuTheme.Accent;
            so.FindProperty("flowActiveColour").colorValue = MenuTheme.Flow;
            so.FindProperty("pipFilledColour").colorValue = MenuTheme.Accent;

            // Empty pips stay a visible shape rather than fading out, so the
            // player can see how many they are missing, not just how many they
            // have.
            so.FindProperty("pipEmptyColour").colorValue = MenuTheme.Track;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
