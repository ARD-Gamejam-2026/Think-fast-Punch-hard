using ThinkFast.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Builds the fighter HUD as real uGUI objects in the open scene.
    ///
    /// The layout is a fighting game's, not a status panel's: both fighters'
    /// health runs across the top of the fight, draining toward the middle, so the
    /// gap between the two bars *is* the score. Each fighter gets a rounded card
    /// with a glove badge overlapping the end of their bar, which is what makes
    /// the two sides read as two players rather than as two meters.
    ///
    /// Flow and action points sit on the player's own card, directly under their
    /// health, rather than in the opposite corner. They are read in the same
    /// glance as the health bar -- "can I swing, and is it worth double" is one
    /// question -- and a fighter's eyes are already at the top of the screen.
    ///
    /// Everything is confined to the fighter's viewport rather than the window, so
    /// nothing lands over the quiz. The split-screen layout drives that, which is
    /// why the ratio lives in one place and this does not know it.
    /// </summary>
    public static class FighterHudBuilder
    {
        private const string RootName = "--- Fighter HUD (generated) ---";

        private const float SideMargin = 34f;
        private const float TopMargin = 24f;

        private const float CardWidth = 500f;

        /// <summary>How far the soft shade spreads past the card that casts it.</summary>
        private const float Elevation = 14f;

        private const float BadgeSize = 92f;

        /// <summary>Distance from the card's outer edge to the badge's centre.</summary>
        private const float BadgeCentreX = 46f;

        /// <summary>Width of the white ring around the badge's coloured disc.</summary>
        private const float BadgeRing = 7f;

        private const float GlyphSize = 52f;

        /// <summary>
        /// Where the health bar starts. Inside the badge's far edge by design --
        /// that overlap is what pins the portrait to the bar instead of leaving
        /// two unrelated shapes in a row.
        /// </summary>
        private const float BarLeft = 78f;

        /// <summary>Where everything that is not the bar starts, clear of the badge.</summary>
        private const float TextLeft = 102f;

        private const float RightPad = 22f;

        private const float NameTop = 12f;
        private const float NameHeight = 24f;

        private const float BarTop = 40f;
        private const float BarHeight = 32f;

        /// <summary>Border of empty track left showing around the fill.</summary>
        private const float BarInset = 4f;

        private const float FlowTop = 80f;
        private const float MeterHeight = 18f;
        private const float MeterWidth = 280f;

        private const float LabelWidth = 56f;
        private const float LabelGap = 10f;

        private const float ApTop = 104f;
        private const float PipSize = 22f;
        private const float PipGap = 10f;

        private const int PipCount = 5;

        private const float PlayerCardHeight = 138f;

        /// <summary>
        /// Shorter than the player's: the opponent has no Flow and no action
        /// points, only the badge and the bar. The two cards are deliberately
        /// different heights -- what you spend is yours, and giving the enemy a
        /// matching empty box would only invite the eye to look for something
        /// there.
        /// </summary>
        private const float OpponentCardHeight = 114f;

        /// <summary>
        /// Where the player's card ends, measured down from the top of the
        /// fighter viewport.
        ///
        /// Public because the tutorial hangs its coach card directly underneath
        /// the HUD, and the two have to agree about where "underneath" starts.
        /// Left to drift, a taller HUD would simply be covered by the card that
        /// is teaching the player to read it.
        /// </summary>
        public const float PlayerCardBottom = TopMargin + PlayerCardHeight;

        [MenuItem("Tools/Think Fast/Build Fighter HUD")]
        public static void Build()
        {
            RemoveExisting();

            UiSpriteFactory.Sprites sprites = UiSpriteFactory.Load();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito SDF.asset");

            GameObject root = CreateCanvas();

            // Registered immediately after creation rather than at the end of the
            // build, which is what every other builder here does and what Unity
            // documents. (The rebuild-deletes-the-HUD bug was not this -- see
            // RemoveExisting -- but registering an object only after it has been
            // fully populated is its own hazard.)
            Undo.RegisterCreatedObjectUndo(root, "Build Fighter HUD");

            // Everything hangs off this, and the split-screen layout anchors it to
            // the fighter's share of the window.
            RectTransform viewport = UiFactory.Stretch(UiFactory.NewRect("Viewport", root.transform));

            RectTransform playerCard = BuildCard(viewport, "Player", true, PlayerCardHeight, sprites);
            Image playerHealth = BuildHealthBar(playerCard, true);
            BuildBadge(playerCard, true, MenuTheme.AccentStrong, sprites);
            BuildName(playerCard, true, "YOU", font);

            Image flowFill = BuildFlowMeter(playerCard, font, out TMP_Text flowLabel);
            Image[] pips = BuildActionPoints(playerCard, sprites, font);

            RectTransform opponentCard = BuildCard(viewport, "Opponent", false, OpponentCardHeight, sprites);
            Image opponentHealth = BuildHealthBar(opponentCard, false);
            BuildBadge(opponentCard, false, MenuTheme.TextPrimary, sprites);
            BuildName(opponentCard, false, "TRAINER", font);

            var hud = root.AddComponent<FighterHud>();
            Wire(hud, playerHealth, opponentHealth, flowFill, flowLabel, pips);

            RegisterWithSplitScreen(viewport);

            Selection.activeGameObject = root;

            // Without this the new HUD is not part of the scene's unsaved state,
            // so it silently vanishes if the scene is reloaded without a save.
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("Built the fighter HUD. It finds both fighters automatically on Play.");
        }

        /// <summary>
        /// Clears the previous HUD.
        ///
        /// **Only scene roots are searched, and that is the fix, not a tidy-up.**
        /// This used to walk every GameObject in the scene and destroy the ones
        /// matching by name. Destroying the HUD root also destroys its children --
        /// which were still sitting in the array being iterated -- so the next
        /// loop read `.name` off a destroyed object and threw. The exception
        /// aborted the build before anything was created, which is why rebuilding
        /// deleted the HUD and put nothing back, while building into a scene that
        /// had none worked fine. Roots cannot contain each other, so the same
        /// mistake is not available here.
        ///
        /// The removal also goes through the undo system, so the stack does not
        /// end up holding a record of an object destroyed behind its back.
        /// </summary>
        private static void RemoveExisting()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == RootName)
                {
                    Undo.DestroyObjectImmediate(go);
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
        /// The corner a fighter's card and its contents hang from. The player
        /// builds out of the top left and the opponent out of the top right, so
        /// every offset below is a distance from *their own* outer edge and the
        /// whole card mirrors for free.
        /// </summary>
        private static Vector2 Corner(bool isPlayer) => new Vector2(isPlayer ? 0f : 1f, 1f);

        /// <summary>Which way <see cref="Corner"/>'s x offsets point.</summary>
        private static float Dir(bool isPlayer) => isPlayer ? 1f : -1f;

        /// <summary>
        /// Places a child at an inset from its fighter's outer top corner, in the
        /// same coordinates for both fighters.
        /// </summary>
        private static RectTransform PlaceIn(
            RectTransform card, string name, bool isPlayer, float x, float y, Vector2 size)
        {
            Vector2 corner = Corner(isPlayer);
            return UiFactory.Place(
                UiFactory.NewRect(name, card),
                corner,
                corner,
                new Vector2(Dir(isPlayer) * x, -y),
                size);
        }

        /// <summary>
        /// One fighter's card: a soft shade, then a rounded panel on top of it.
        ///
        /// The shade is not decoration. A white card straight onto a lit 3D stage
        /// reads as a hole cut in the screen, and the HUD has to keep its contrast
        /// over whatever colour the level behind it turns out to be.
        /// </summary>
        private static RectTransform BuildCard(
            RectTransform parent, string name, bool isPlayer, float height, UiSpriteFactory.Sprites sprites)
        {
            Vector2 corner = Corner(isPlayer);

            RectTransform root = UiFactory.Place(
                UiFactory.NewRect(name, parent),
                corner,
                corner,
                new Vector2(Dir(isPlayer) * SideMargin, -TopMargin),
                new Vector2(CardWidth, height));

            RectTransform shade = UiFactory.Stretch(UiFactory.NewRect("Shade", root), -Elevation);
            UiFactory.AddImage(shade, sprites.CardGlow, MenuTheme.Shadow);

            RectTransform card = UiFactory.Stretch(UiFactory.NewRect("Card", root));
            UiFactory.AddImage(card, sprites.Card, MenuTheme.HudCard);

            return card;
        }

        /// <summary>
        /// The fighter's name, set clear of the badge and aligned to their own
        /// side, so the two names read outward from the middle of the screen.
        /// </summary>
        private static void BuildName(RectTransform card, bool isPlayer, string title, TMP_FontAsset font)
        {
            TMP_Text label = UiFactory.AddLabel(
                card,
                "Name",
                title,
                22f,
                MenuTheme.TextPrimary,
                font,
                FontStyles.Bold,
                isPlayer ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);

            Vector2 corner = Corner(isPlayer);
            UiFactory.Place(
                label.rectTransform,
                corner,
                corner,
                new Vector2(Dir(isPlayer) * TextLeft, -NameTop),
                new Vector2(CardWidth - TextLeft - RightPad, NameHeight));
        }

        /// <summary>
        /// One fighter's health bar. Square ends, matching the menu's sliders: at
        /// this height a fully rounded track is all cap and no middle, and a
        /// square fill inside a rounded track leaves a wedge of track showing in
        /// each corner when it is full.
        /// </summary>
        private static Image BuildHealthBar(RectTransform card, bool isPlayer)
        {
            RectTransform track = PlaceIn(
                card,
                "Health Track",
                isPlayer,
                BarLeft,
                BarTop,
                new Vector2(CardWidth - BarLeft - RightPad, BarHeight));

            UiFactory.AddImage(track, null, MenuTheme.Track);

            return BuildFill(track, MenuTheme.Positive);
        }

        /// <summary>
        /// The moving part of a bar, inset so a rim of empty track always frames
        /// it. Offsets rather than a sprite, because <see cref="FighterHud"/>
        /// drives the level by anchor and would fight a sliced image.
        /// </summary>
        private static Image BuildFill(RectTransform track, Color colour)
        {
            RectTransform fill = UiFactory.NewRect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(BarInset, BarInset);
            fill.offsetMax = new Vector2(-BarInset, -BarInset);

            return UiFactory.AddImage(fill, null, colour);
        }

        /// <summary>
        /// The fighter's badge: a white ring, a coloured disc and a glove.
        ///
        /// It overlaps the inner end of the health bar rather than sitting beside
        /// it, which is what makes the pair read as one fighter's readout. The
        /// ring is doing real work -- it is the only thing keeping the disc off
        /// the track underneath it -- and the glove is mirrored for the opponent
        /// so the two face each other across the screen.
        /// </summary>
        private static void BuildBadge(
            RectTransform card, bool isPlayer, Color disc, UiSpriteFactory.Sprites sprites)
        {
            float centreY = BarTop + (BarHeight * 0.5f);

            RectTransform badge = PlaceIn(
                card,
                "Badge",
                isPlayer,
                BadgeCentreX - (BadgeSize * 0.5f),
                centreY - (BadgeSize * 0.5f),
                new Vector2(BadgeSize, BadgeSize));

            RectTransform ring = UiFactory.Stretch(UiFactory.NewRect("Ring", badge));
            UiFactory.AddImage(ring, sprites.Circle, MenuTheme.Surface).type = Image.Type.Simple;

            RectTransform face = UiFactory.Stretch(UiFactory.NewRect("Disc", badge), BadgeRing);
            UiFactory.AddImage(face, sprites.Circle, disc).type = Image.Type.Simple;

            RectTransform glyph = UiFactory.Place(
                UiFactory.NewRect("Glove", badge),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(GlyphSize, GlyphSize));

            UiFactory.AddImage(glyph, sprites.Glove, MenuTheme.TextOnAccent).type = Image.Type.Simple;

            // Mirrored rather than drawn twice. Scaling the glyph and not the
            // badge keeps the ring and disc perfectly circular either way.
            glyph.localScale = new Vector3(Dir(isPlayer), 1f, 1f);
        }

        private static Image BuildFlowMeter(RectTransform card, TMP_FontAsset font, out TMP_Text label)
        {
            label = UiFactory.AddLabel(
                card, "Flow Label", "FLOW", 18f, MenuTheme.TextSecondary, font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            UiFactory.Place(
                label.rectTransform,
                Corner(true),
                Corner(true),
                new Vector2(TextLeft, -FlowTop),
                new Vector2(LabelWidth, MeterHeight));

            RectTransform track = PlaceIn(
                card, "Flow Track", true, TextLeft + LabelWidth + LabelGap, FlowTop, new Vector2(MeterWidth, MeterHeight));
            UiFactory.AddImage(track, null, MenuTheme.Track);

            return BuildFill(track, MenuTheme.Accent);
        }

        private static Image[] BuildActionPoints(RectTransform card, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            TMP_Text label = UiFactory.AddLabel(
                card, "AP Label", "AP", 18f, MenuTheme.TextSecondary, font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            UiFactory.Place(
                label.rectTransform,
                Corner(true),
                Corner(true),
                new Vector2(TextLeft, -ApTop),
                new Vector2(LabelWidth, PipSize));

            float pipLeft = TextLeft + LabelWidth + LabelGap;

            var pips = new Image[PipCount];
            for (int i = 0; i < PipCount; i++)
            {
                RectTransform pip = PlaceIn(
                    card, $"Pip {i}", true, pipLeft + (i * (PipSize + PipGap)), ApTop, new Vector2(PipSize, PipSize));

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

        private static void Wire(
            FighterHud hud, Image playerHealth, Image opponentHealth, Image flowFill, TMP_Text flowLabel, Image[] pips)
        {
            var so = new SerializedObject(hud);

            so.FindProperty("healthFill").objectReferenceValue = playerHealth.rectTransform;
            so.FindProperty("healthFillImage").objectReferenceValue = playerHealth;
            so.FindProperty("opponentHealthFill").objectReferenceValue = opponentHealth.rectTransform;
            so.FindProperty("opponentHealthFillImage").objectReferenceValue = opponentHealth;
            so.FindProperty("flowFill").objectReferenceValue = flowFill.rectTransform;
            so.FindProperty("flowFillImage").objectReferenceValue = flowFill;
            so.FindProperty("flowLabel").objectReferenceValue = flowLabel;

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

            // The label goes with its bar: muted while Flow is only filling, and
            // ink-dark the moment the state is live, so the word is legible at the
            // one point it means something. Not the bar's own amber -- that is a
            // fill colour, and 2.2:1 as type on a white card.
            so.FindProperty("flowLabelColour").colorValue = MenuTheme.TextSecondary;
            so.FindProperty("flowLabelActiveColour").colorValue = MenuTheme.TextPrimary;

            // Empty pips stay a visible shape rather than fading out, so the player
            // can see how many they are missing, not just how many they have.
            so.FindProperty("pipEmptyColour").colorValue = MenuTheme.Track;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
