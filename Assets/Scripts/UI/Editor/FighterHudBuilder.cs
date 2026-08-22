using ThinkFast.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Builds the fighter HUD as real uGUI objects in the open scene.
    ///
    /// The layout is a fighting game's, not a status panel's: both fighters'
    /// health runs across the top of the fight, draining toward the middle, so the
    /// gap between the two bars *is* the score. Your own resources -- Flow and
    /// action points -- sit apart from that in the bottom corner, because they are
    /// something you spend rather than something you are losing.
    ///
    /// Everything is confined to the fighter's viewport rather than the window, so
    /// nothing lands over the quiz. The split-screen layout drives that, which is
    /// why the ratio lives in one place and this does not know it.
    /// </summary>
    public static class FighterHudBuilder
    {
        private const string RootName = "--- Fighter HUD (generated) ---";

        private const float SideMargin = 34f;
        private const float TopMargin = 26f;

        private const float HealthBarHeight = 30f;
        private const float HealthBarWidth = 400f;
        private const float NameHeight = 30f;

        private const float MeterWidth = 260f;
        private const float MeterHeight = 18f;
        private const float PipSize = 20f;
        private const float PipGap = 8f;

        [MenuItem("Tools/Think Fast/Build Fighter HUD")]
        public static void Build()
        {
            RemoveExisting();

            UiSpriteFactory.Sprites sprites = UiSpriteFactory.Load();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito SDF.asset");

            GameObject root = CreateCanvas();

            // Everything hangs off this, and the split-screen layout anchors it to
            // the fighter's share of the window.
            RectTransform viewport = UiFactory.Stretch(UiFactory.NewRect("Viewport", root.transform));

            Image playerHealth = BuildHealthBar(viewport, "Player Health", "YOU", true, sprites, font);
            Image opponentHealth = BuildHealthBar(viewport, "Opponent Health", "TRAINER", false, sprites, font);

            Image flowFill = BuildFlowMeter(viewport, sprites, font);
            Image[] pips = BuildActionPoints(viewport, 5, sprites, font);

            var hud = root.AddComponent<FighterHud>();
            Wire(hud, playerHealth, opponentHealth, flowFill, pips);

            RegisterWithSplitScreen(viewport);

            Undo.RegisterCreatedObjectUndo(root, "Build Fighter HUD");
            Selection.activeGameObject = root;

            // Without this the new HUD is not part of the scene's unsaved state,
            // so it silently vanishes if the scene is reloaded without a save.
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("Built the fighter HUD. It finds both fighters automatically on Play.");
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

            // Above the quiz backdrop, level with the quiz panel. They occupy
            // different halves, so the order between them never comes up.
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

        /// <summary>
        /// One fighter's health: a name above a bar, pinned to its own top corner.
        /// The player's drains to the right and the opponent's to the left, so the
        /// two empty toward each other.
        /// </summary>
        private static Image BuildHealthBar(
            RectTransform parent, string name, string title, bool isPlayer, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            Vector2 anchor = new Vector2(isPlayer ? 0f : 1f, 1f);
            float x = isPlayer ? SideMargin : -SideMargin;

            RectTransform group = UiFactory.Place(
                UiFactory.NewRect(name, parent),
                anchor,
                anchor,
                new Vector2(x, -TopMargin),
                new Vector2(HealthBarWidth, NameHeight + HealthBarHeight + 4f));

            TextAlignmentOptions align = isPlayer ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            TMP_Text label = UiFactory.AddLabel(group, "Name", title, 24f, MenuTheme.TextPrimary, font, FontStyles.Bold, align);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(HealthBarWidth, NameHeight));

            RectTransform track = UiFactory.Place(
                UiFactory.NewRect("Track", group),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(HealthBarWidth, HealthBarHeight));

            // The track is opaque, not a tint of whatever is behind it. A health
            // bar has to be readable against a stage nobody has built yet.
            Image trackImage = UiFactory.AddImage(track, sprites.Pill, MenuTheme.Surface);
            trackImage.type = Image.Type.Sliced;

            RectTransform fill = UiFactory.NewRect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);

            Image fillImage = UiFactory.AddImage(fill, sprites.Pill, MenuTheme.Positive);
            fillImage.type = Image.Type.Sliced;

            return fillImage;
        }

        private static Image BuildFlowMeter(RectTransform parent, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            RectTransform group = UiFactory.Place(
                UiFactory.NewRect("Flow", parent),
                Vector2.zero,
                Vector2.zero,
                new Vector2(SideMargin, 74f),
                new Vector2(MeterWidth, MeterHeight));

            TMP_Text label = UiFactory.AddLabel(group, "Label", "FLOW", 20f, MenuTheme.TextMuted, font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(70f, MeterHeight));

            RectTransform track = UiFactory.Place(
                UiFactory.NewRect("Track", group),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(78f, 0f),
                new Vector2(MeterWidth - 78f, MeterHeight));
            UiFactory.AddImage(track, sprites.Pill, MenuTheme.Surface);

            RectTransform fill = UiFactory.NewRect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);

            return UiFactory.AddImage(fill, sprites.Pill, MenuTheme.Accent);
        }

        private static Image[] BuildActionPoints(RectTransform parent, int count, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            RectTransform group = UiFactory.Place(
                UiFactory.NewRect("Action Points", parent),
                Vector2.zero,
                Vector2.zero,
                new Vector2(SideMargin, 34f),
                new Vector2(MeterWidth, PipSize));

            TMP_Text label = UiFactory.AddLabel(group, "Label", "AP", 20f, MenuTheme.TextMuted, font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(70f, PipSize));

            var pips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform pip = UiFactory.Place(
                    UiFactory.NewRect($"Pip {i}", group),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(78f + (i * (PipSize + PipGap)), 0f),
                    new Vector2(PipSize, PipSize));

                Image image = UiFactory.AddImage(pip, sprites.Circle, MenuTheme.Accent);
                image.type = Image.Type.Simple;
                pips[i] = image;
            }

            return pips;
        }

        /// <summary>
        /// Hands the HUD's root to the split-screen layout, which anchors it to the
        /// fighter's share of the window. Without this the top bars would span the
        /// whole screen and run across the quiz.
        /// </summary>
        private static void RegisterWithSplitScreen(RectTransform viewport)
        {
            var layout = Object.FindAnyObjectByType<SplitScreenLayout>();
            if (layout == null)
            {
                Debug.LogWarning("No SplitScreenLayout in the scene, so the HUD will span the whole window. Run Tools > Think Fast > Build Split Screen Fight.");
                return;
            }

            var so = new SerializedObject(layout);
            so.FindProperty("fighterHud").objectReferenceValue = viewport;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Re-applied because the layout already ran once, before this HUD
            // existed to be anchored. Without it the Game view shows the bars
            // spanning the whole window until Play is pressed.
            layout.Apply();
        }

        private static void Wire(FighterHud hud, Image playerHealth, Image opponentHealth, Image flowFill, Image[] pips)
        {
            var so = new SerializedObject(hud);

            so.FindProperty("healthFill").objectReferenceValue = playerHealth.rectTransform;
            so.FindProperty("healthFillImage").objectReferenceValue = playerHealth;
            so.FindProperty("opponentHealthFill").objectReferenceValue = opponentHealth.rectTransform;
            so.FindProperty("opponentHealthFillImage").objectReferenceValue = opponentHealth;
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

            // Empty pips stay a visible shape rather than fading out, so the player
            // can see how many they are missing, not just how many they have.
            so.FindProperty("pipEmptyColour").colorValue = MenuTheme.Track;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
