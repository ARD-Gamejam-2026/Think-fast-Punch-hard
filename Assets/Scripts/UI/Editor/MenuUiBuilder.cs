using ThinkFast.Menu;
using ThinkFast.Rounds;
using ThinkFast.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Builds the start and end screens: a channel grid of cards on a near-white
    /// page, a settings bar along the bottom, and a wash between screens.
    ///
    /// Both screens are generated from one description so they cannot drift into
    /// looking like two different games, which is the usual fate of a menu and a
    /// results screen built at different times by different people.
    /// </summary>
    public static class MenuUiBuilder
    {
        private const string MenuScenePath = "Assets/Scenes/Scene_Menu.unity";
        private const string EndScenePath = "Assets/Scenes/Scene_End.unity";

        private const string FightSceneName = "PlayerControllerTest";
        private const string MenuSceneName = "Scene_Menu";

        private const string MenuRootName = "--- Menu UI (generated) ---";
        private const string EndRootName = "--- End UI (generated) ---";

        /// <summary>Kept so the round flow's end-screen wiring still finds this label by name.</summary>
        private const string EndMessageName = "TMP_EndMessage";

        private const string MixerPath = "Assets/AudioMixer_Main.mixer";
        private const string AudioManagerPath = "Assets/ScriptableObjects/SO_AudioManager.asset";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 TileSize = new Vector2(430f, 320f);

        /// <summary>One volume slider: caption column plus bar.</summary>
        private static readonly Vector2 SliderSize = new Vector2(520f, 56f);

        /// <summary>Width of the caption column inside a slider.</summary>
        private const float SliderCaptionWidth = 120f;

        /// <summary>Gap between the two sliders in the settings bar.</summary>
        private const float SliderGap = 90f;

        [MenuItem("Tools/Think Fast/Build Menu UI")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            TMP_FontAsset font = MenuFontFactory.EnsureFontAsset();
            UiSpriteFactory.Sprites sprites = UiSpriteFactory.BuildAll();

            if (!sprites.IsComplete)
            {
                Debug.LogError("The menu sprites could not be generated, so the screens were not built.");
                return;
            }

            BuildMenuScene(sprites, font);
            BuildEndScene(sprites, font);

            Debug.Log("Built the start and end screens. Open Scene_Menu and press Play.");
        }

        private static void BuildMenuScene(UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            ReplaceOldUi(scene, MenuRootName);

            RectTransform canvas = CreateCanvas(MenuRootName, out GameObject root);
            AddBackdrop(canvas);

            AddTitle(canvas, font);
            AddTileRow(canvas, 2, out RectTransform[] slots);

            Button start = UiFactory.MakeCard(slots[0], "Tile_Start", "START", TileSize, sprites, font, 40f, out _);
            SetSceneTarget(start, FightSceneName);

            Button help = UiFactory.MakeCard(slots[1], "Tile_HowToPlay", "HOW TO PLAY", TileSize, sprites, font, 40f, out _);

            // The bar first, so the panel that has to cover it is created after
            // it: on a canvas, later siblings draw on top. A modal that the
            // settings bar punched through would still be clickable underneath.
            AddSettingsBar(canvas, sprites, font);

            GameObject panel = AddHowToPlayPanel(canvas, sprites, font, help.gameObject);
            SetPanelTarget(help, panel, opens: true, focusAfter: FindCloseButton(panel));

            AddFade(canvas, root);
            AddSounds(root);

            SelectFirst(root, start.gameObject);
            Save(scene);
        }

        private static void BuildEndScene(UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(EndScenePath, OpenSceneMode.Single);
            ReplaceOldUi(scene, EndRootName);

            RectTransform canvas = CreateCanvas(EndRootName, out GameObject root);
            AddBackdrop(canvas);

            // Named for the round flow, which finds it by name to write the
            // outcome into. Renaming it here silently breaks that wiring.
            TMP_Text message = UiFactory.AddLabel(
                canvas, EndMessageName, "You mastered your Body and Mind", 68f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            UiFactory.Place(
                message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1360f, 220f));

            AddTileRow(canvas, 2, out RectTransform[] slots);

            Button again = UiFactory.MakeCard(slots[0], "Tile_FightAgain", "FIGHT AGAIN", TileSize, sprites, font, 38f, out _);
            SetSceneTarget(again, FightSceneName);

            Button menu = UiFactory.MakeCard(slots[1], "Tile_Menu", "MENU", TileSize, sprites, font, 40f, out _);
            SetSceneTarget(menu, MenuSceneName);

            AddFade(canvas, root);
            AddSounds(root);
            RepointEndScreen(message);

            SelectFirst(root, again.gameObject);
            Save(scene);
        }

        /// <summary>
        /// Removes the previous generated root, and the hand-built canvas this
        /// replaces. Deleting rather than hiding the old one is deliberate: two
        /// canvases with the same object names would leave the round flow's
        /// find-by-name wiring picking between them.
        /// </summary>
        private static void ReplaceOldUi(Scene scene, string rootName)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == rootName || go.name == "CanvasMenuUI")
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static RectTransform CreateCanvas(string name, out GameObject root)
        {
            root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Build Menu UI");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            // Match height: these screens are laid out vertically and a wide
            // window should get more room either side, not larger type.
            scaler.matchWidthOrHeight = 1f;

            return (RectTransform)root.transform;
        }

        private static void AddBackdrop(RectTransform canvas)
        {
            RectTransform rect = UiFactory.Stretch(UiFactory.NewRect("Backdrop", canvas));
            UiFactory.AddImage(rect, null, MenuTheme.Background, raycast: true);
        }

        private static void AddTitle(RectTransform canvas, TMP_FontAsset font)
        {
            TMP_Text title = UiFactory.AddLabel(
                canvas, "Title", "Think Fast, Punch Hard", 104f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            UiFactory.Place(
                title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1600f, 140f));

            TMP_Text tagline = UiFactory.AddLabel(
                canvas, "Tagline", "Sharpen the mind. The body follows.", 34f, MenuTheme.TextMuted, font);
            UiFactory.Place(
                tagline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(1600f, 50f));
        }

        /// <summary>
        /// Lays out evenly spaced slots for the tiles. Each tile gets its own slot
        /// rect so it can be scaled on hover without a layout group fighting it
        /// for position.
        /// </summary>
        private static RectTransform AddTileRow(RectTransform canvas, int count, out RectTransform[] slots)
        {
            const float Spacing = 56f;

            RectTransform row = UiFactory.Place(
                UiFactory.NewRect("Tiles", canvas),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f),
                new Vector2((TileSize.x * count) + (Spacing * (count - 1)), TileSize.y));

            slots = new RectTransform[count];
            float step = TileSize.x + Spacing;
            float first = -((step * (count - 1)) * 0.5f);

            for (int i = 0; i < count; i++)
            {
                slots[i] = UiFactory.Place(
                    UiFactory.NewRect($"Slot {i}", row),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(first + (step * i), 0f),
                    TileSize);
            }

            return row;
        }

        private static void AddSettingsBar(RectTransform canvas, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            RectTransform bar = UiFactory.NewRect("SettingsBar", canvas);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 128f);
            UiFactory.AddImage(bar, null, MenuTheme.Surface, raycast: true);

            // A hairline instead of a shadow: the bar is white on near-white, and
            // one line is enough to separate them without adding weight.
            RectTransform edge = UiFactory.NewRect("TopEdge", bar);
            edge.anchorMin = new Vector2(0f, 1f);
            edge.anchorMax = new Vector2(1f, 1f);
            edge.pivot = new Vector2(0.5f, 1f);
            edge.anchoredPosition = Vector2.zero;
            edge.sizeDelta = new Vector2(0f, 2f);
            UiFactory.AddImage(edge, null, MenuTheme.Border);

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var manager = AssetDatabase.LoadAssetAtPath<AudioManager>(AudioManagerPath);

            // Positions are derived rather than typed in, so the pair stays
            // centred as a group whatever the widths become. Each x is the centre
            // of one slider, half a step either side of the middle.
            float step = SliderSize.x + SliderGap;
            AddVolumeSlider(bar, "Slider_Music", "Music", -step * 0.5f, VolumeSlider.Channel.Music, sprites, font, mixer, manager);
            AddVolumeSlider(bar, "Slider_Sfx", "Sound", step * 0.5f, VolumeSlider.Channel.Sfx, sprites, font, mixer, manager);

            if (mixer == null || manager == null)
            {
                Debug.LogWarning("The audio mixer or manager asset is missing, so the volume sliders will move but change nothing.");
            }
        }

        private static void AddVolumeSlider(
            RectTransform bar,
            string name,
            string caption,
            float x,
            VolumeSlider.Channel channel,
            UiSpriteFactory.Sprites sprites,
            TMP_FontAsset font,
            AudioMixer mixer,
            AudioManager manager)
        {
            Slider slider = UiFactory.MakeSlider(bar, name, caption, SliderSize, SliderCaptionWidth, sprites, font);

            // The Slider component sits on the bar rect, one level inside the
            // slider's own root -- it is the root that gets positioned.
            var root = slider.transform.parent as RectTransform;
            UiFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), SliderSize);

            var volume = slider.gameObject.AddComponent<VolumeSlider>();
            var so = new SerializedObject(volume);
            so.FindProperty("channel").enumValueIndex = (int)channel;
            so.FindProperty("audioManager").objectReferenceValue = manager;
            so.FindProperty("mixer").objectReferenceValue = mixer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject AddHowToPlayPanel(
            RectTransform canvas, UiSpriteFactory.Sprites sprites, TMP_FontAsset font, GameObject returnFocusTo)
        {
            RectTransform panel = UiFactory.Stretch(UiFactory.NewRect("Panel_HowToPlay", canvas));

            // The dim catches every click outside the card, so the panel is modal
            // without anything having to disable what is behind it.
            UiFactory.AddImage(panel, null, new Color(0.18f, 0.22f, 0.26f, 0.32f), raycast: true);

            RectTransform card = UiFactory.Place(
                UiFactory.NewRect("Card", panel),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(940f, 640f));
            UiFactory.AddImage(card, sprites.Card, MenuTheme.Surface, raycast: true);

            TMP_Text heading = UiFactory.AddLabel(card, "Heading", "How to play", 52f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            UiFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(760f, 70f));

            TMP_Text body = UiFactory.AddLabel(
                card, "Body", HowToPlayText(), 30f, MenuTheme.TextPrimary, font, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            UiFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(800f, 430f));

            Button close = UiFactory.MakeRound(card, "Button_Close", "X", 64f, sprites, font);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(64f, 64f));
            SetPanelTarget(close, panel.gameObject, opens: false, focusAfter: returnFocusTo);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static string HowToPlayText()
        {
            return
                "Your body fights on the left. Your mind answers on the right.\n\n" +
                "<b>A</b> / <b>D</b>   move        <b>W</b>   jump\n" +
                "<b>S</b>   fast fall, or drop through a platform\n" +
                "<b>Space</b>   punch  (costs one action point)\n\n" +
                "Answer a question correctly to earn an action point.\n" +
                "Answer it <b>while the timer bar is still green</b> to earn Flow.\n" +
                "Fill the Flow meter and your punches hit twice as hard — " +
                "but every swing spends it, so make the window count.";
        }

        private static void AddFade(RectTransform canvas, GameObject root)
        {
            // Last child, so it covers everything else on the canvas.
            RectTransform rect = UiFactory.Stretch(UiFactory.NewRect("Fade", canvas));
            Image sheet = UiFactory.AddImage(rect, null, MenuTheme.FadeSheet, raycast: true);
            rect.SetAsLastSibling();

            var fade = root.AddComponent<ScreenFade>();
            var so = new SerializedObject(fade);
            so.FindProperty("sheet").objectReferenceValue = sheet;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSounds(GameObject root)
        {
            var sounds = root.AddComponent<MenuSounds>();
            AudioMixerGroup group = FindSfxGroup();

            var so = new SerializedObject(sounds);
            so.FindProperty("output").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (group == null)
            {
                Debug.LogWarning("No SFX mixer group found, so menu sounds will play outside the volume slider's control.");
            }
        }

        private static AudioMixerGroup FindSfxGroup()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                return null;
            }

            AudioMixerGroup[] groups = mixer.FindMatchingGroups("SFX");
            if (groups == null || groups.Length == 0)
            {
                return null;
            }

            return groups[0];
        }

        private static void SetSceneTarget(Button button, string sceneName)
        {
            var load = button.gameObject.AddComponent<LoadSceneButton>();
            var so = new SerializedObject(load);
            so.FindProperty("sceneName").stringValue = sceneName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPanelTarget(Button button, GameObject panel, bool opens, GameObject focusAfter)
        {
            var toggle = button.gameObject.AddComponent<TogglePanelButton>();
            var so = new SerializedObject(toggle);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("opens").boolValue = opens;
            so.FindProperty("focusAfter").objectReferenceValue = focusAfter;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindCloseButton(GameObject panel)
        {
            Transform close = panel.transform.Find("Card/Button_Close");
            if (close == null)
            {
                return null;
            }

            return close.gameObject;
        }

        /// <summary>
        /// Points the end screen's outcome label at the one just built. The round
        /// flow wired it to the label in the old canvas, which has been replaced.
        /// </summary>
        private static void RepointEndScreen(TMP_Text message)
        {
            var endScreen = Object.FindAnyObjectByType<EndScreen>();
            if (endScreen == null)
            {
                Debug.LogWarning("No EndScreen in the end scene. Run Tools > Think Fast > Build Round Flow to add it.");
                return;
            }

            var so = new SerializedObject(endScreen);
            so.FindProperty("message").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Gives the first tile keyboard focus on load, so arrow keys work without
        /// the player having to click something first.
        /// </summary>
        private static void SelectFirst(GameObject root, GameObject first)
        {
            var events = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (events == null || first == null)
            {
                return;
            }

            var so = new SerializedObject(events);
            so.FindProperty("m_FirstSelected").objectReferenceValue = first;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
