using System.Collections.Generic;
using ThinkFast.CameraRig;
using ThinkFast.Economy;
using ThinkFast.Enemy;
using ThinkFast.Player;
using ThinkFast.PlayerEditor;
using ThinkFast.Quiz;
using ThinkFast.Tutorial;
using ThinkFast.UI;
using ThinkFast.UIEditor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.TutorialEditor
{
    /// <summary>
    /// Builds the tutorial scene from nothing, every time.
    ///
    /// Nothing in it is hand-authored, so it is created rather than opened and
    /// patched: a scene that is entirely generated cannot drift from the script
    /// that generates it, and the whole thing is reviewable as code. Re-running
    /// this command is always safe.
    ///
    /// What it assembles is a stripped-down version of the real fight -- same
    /// split, same HUD, same fighter, same quiz panel -- with three deliberate
    /// differences:
    ///
    /// * the opponent is a <see cref="TutorialDummy"/> that never fights back,
    /// * there is no round flow, so nothing can end and nothing loads an end
    ///   screen,
    /// * the quiz stays shut until the step that introduces it.
    ///
    /// Order matters in two places. The split-screen layout has to exist before
    /// the HUD, which registers itself with it; and the HUD has to exist before
    /// the coach card, which is parented inside the HUD's viewport so it is
    /// confined to the fight's half of the window without knowing the ratio.
    /// </summary>
    public static class TutorialSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Scene_Tutorial.unity";
        private const string QuizPrefabPath = "Assets/Quiz/QuizPanel.prefab";
        private const string FontPath = "Assets/Fonts/Nunito SDF.asset";

        private const string StageRootName = "--- Tutorial Stage (generated) ---";
        private const string QuizRootName = "--- Quiz (generated) ---";
        private const string SplitRootName = "--- Split Screen (generated) ---";
        private const string TutorialRootName = "--- Tutorial (generated) ---";

        private const string PanelContainerName = "Container";

        private static readonly Vector3 PlayerSpawn = new Vector3(-3f, 1.2f, 0f);
        private static readonly Vector3 DummySpawn = new Vector3(3f, 1.2f, 0f);

        /// <summary>The coach card, in the HUD canvas's reference pixels.</summary>
        private static readonly Vector2 CardSize = new Vector2(1060f, 300f);

        /// <summary>
        /// Gap between the top of the fight's viewport and the card.
        ///
        /// Load-bearing, with the card height: together they put the card's
        /// bottom edge at roughly 64% of the view, and the camera holds the
        /// fighter's head below that. Growing the card downward is what starts
        /// covering the fight.
        /// </summary>
        private const float TopMargin = 92f;

        /// <summary>Inner margin of the card. Generous on purpose -- crowding is what made it read as a debug panel.</summary>
        private const float Pad = 44f;

        /// <summary>How far the soft shade spreads past the card's edge.</summary>
        private const float Elevation = 14f;

        private static readonly Vector2 ButtonSize = new Vector2(250f, 56f);
        private static readonly Vector2 SkipSize = new Vector2(200f, 36f);

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        [MenuItem("Tools/Think Fast/Build Tutorial Scene")]
        public static void Build()
        {
            // Skipped in batch mode, where there is nobody to ask and the prompt
            // is what a headless run would hang on.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject quizPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuizPrefabPath);
            if (quizPrefab == null)
            {
                Debug.LogError($"No quiz panel at {QuizPrefabPath}. Run Tools > Quiz > Create Quiz Panel first.");
                return;
            }

            UiSpriteFactory.Sprites sprites = UiSpriteFactory.BuildAll();
            if (!sprites.IsComplete)
            {
                Debug.LogError("The UI sprites could not be generated, so the tutorial was not built.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject player = BuildStageAndFighters(out GameObject dummy);

            QuizController quiz = BuildQuizHalf(quizPrefab, out GameObject panel, out QuizFlow flow);
            SplitScreenLayout layout = BuildSplitScreen(quiz, sprites, font, out GameObject placeholder);

            EnsureEventSystem();
            FrameCamera(player.transform);

            // The layout runs before the HUD exists, so the HUD re-applies it
            // when it registers itself. Applying here as well is what makes the
            // Game view show the real split without entering Play mode.
            layout.Apply();

            FighterHudBuilder.Build();

            RectTransform viewport = FindHudViewport();
            TutorialCoach coach = BuildCoach(viewport, sprites, font);

            BuildDirector(coach, player, dummy, quiz, flow, panel, placeholder, layout);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuildSettings();

            Debug.Log($"Built the tutorial in {ScenePath} and added it to Build Settings. It is reachable from the menu's TUTORIAL card once Tools > Think Fast > Build Menu UI has been re-run.");
        }

        /// <summary>
        /// A small flat arena with one ledge: enough to teach jumping and
        /// dropping through, small enough that a new player cannot walk away
        /// from the trainer and lose track of it.
        /// </summary>
        private static GameObject BuildStageAndFighters(out GameObject dummy)
        {
            var root = new GameObject(StageRootName);

            PhysicsMaterial2D frictionless = FighterStageParts.GetOrCreateFrictionlessMaterial();

            FighterStageParts.CreateBlock(root.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 4f), frictionless);
            FighterStageParts.CreateBlock(root.transform, "Wall Left", new Vector3(-11f, 2.5f, 0f), new Vector3(1f, 6f, 4f), frictionless);
            FighterStageParts.CreateBlock(root.transform, "Wall Right", new Vector3(11f, 2.5f, 0f), new Vector3(1f, 6f, 4f), frictionless);

            // One platform, at the same height as the fight's low ones, so what
            // a jump feels like here is what it feels like there.
            FighterStageParts.CreatePlatform(root.transform, "Platform Ledge", new Vector3(-6.5f, 2.4f, 0f), new Vector3(4.5f, 0.4f, 4f), frictionless);

            GameObject player = SpawnFighter(FighterPrefabs.PlayerPath, root.transform, PlayerSpawn, "Player");
            dummy = SpawnFighter(FighterPrefabs.EnemyPath, root.transform, DummySpawn, "Trainer");

            QuietenDebugComponents(player);
            QuietenDebugComponents(dummy);
            MakeDummy(dummy, player.transform);

            return player;
        }

        private static GameObject SpawnFighter(string prefabPath, Transform parent, Vector3 position, string name)
        {
            GameObject prefab = FighterPrefabs.Load(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"No fighter prefab at {prefabPath}. Run Tools > Think Fast > Save Fighters As Prefabs first.");
                return new GameObject(name);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position;
            return instance;
        }

        /// <summary>
        /// Turns the opponent prefab into a punching bag: the component that
        /// keeps it standing still and standing up, and the target it should
        /// turn to face.
        /// </summary>
        private static void MakeDummy(GameObject dummy, Transform player)
        {
            var brain = dummy.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                // Cleared as well as disabled: a brain holding a target is a
                // brain someone will later re-enable by accident and wonder why
                // the tutorial started fighting back.
                SetObjectField(brain, "target", null);
                brain.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
            }

            var attack = dummy.GetComponent<EnemyAttack>();
            if (attack != null)
            {
                attack.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(attack);
            }

            var tutorialDummy = dummy.AddComponent<TutorialDummy>();
            SetObjectField(tutorialDummy, "faceTarget", player);
        }

        /// <summary>
        /// Switches off the throwaway debug affordances the fighter prefabs
        /// carry. A tutorial is the one screen where an IMGUI readout over the
        /// stage, a self-damage key and a stand-in that hands out Flow for free
        /// are all actively misleading.
        /// </summary>
        private static void QuietenDebugComponents(GameObject fighter)
        {
            DisableIfPresent<PlayerDebugHud>(fighter);
            DisableIfPresent<DebugSelfDamage>(fighter);
            DisableIfPresent<DebugRiddleDriver>(fighter);
            DisableIfPresent<EnemyDebugHud>(fighter);
        }

        private static void DisableIfPresent<T>(GameObject target) where T : MonoBehaviour
        {
            var component = target.GetComponent<T>();
            if (component == null || !component.enabled)
            {
                return;
            }

            component.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        /// <summary>
        /// The quiz half: the same panel prefab the fight uses, the same reward
        /// bridge, and a flow that only ever asks the tutorial's own questions.
        ///
        /// The flow starts disabled. A component's Start runs the first time it
        /// is enabled, so that one flag is also what holds the first question
        /// back until the step that introduces the quiz.
        /// </summary>
        private static QuizController BuildQuizHalf(GameObject prefab, out GameObject panel, out QuizFlow flow)
        {
            var root = new GameObject(QuizRootName);

            panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            panel.transform.SetParent(root.transform, worldPositionStays: false);

            var quiz = panel.GetComponent<QuizController>();
            if (quiz == null)
            {
                Debug.LogError("The quiz panel prefab has no QuizController on its root.", panel);
                flow = null;
                return null;
            }

            SetObjectField(quiz, "startingQuestion", null);
            PrefabUtility.RecordPrefabInstancePropertyModifications(quiz);

            flow = AddTutorialFlow(root, quiz);
            AddRewardBridge(root, quiz, panel.GetComponentInChildren<QuizView>());

            // Shut in the saved scene as well as at runtime, so the controller
            // never wakes behind the movement lesson and the Game view shows what
            // the player will actually see on load.
            panel.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(panel.transform);

            return quiz;
        }

        private static QuizFlow AddTutorialFlow(GameObject root, QuizController quiz)
        {
            var flow = root.AddComponent<QuizFlow>();
            flow.enabled = false;

            List<QuizQuestion> questions = TutorialQuestions.EnsureAll();

            var so = new SerializedObject(flow);
            so.FindProperty("quiz").objectReferenceValue = quiz;

            SerializedProperty list = so.FindProperty("questions");
            list.arraySize = questions.Count;
            for (int i = 0; i < questions.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = questions[i];
            }

            // Authored only. The fight's maths, sequence and Wikipedia questions
            // are all fine questions and all wrong here: the tutorial needs to
            // know how hard the question in front of the player is, because the
            // Flow lesson depends on them being able to answer it in the green.
            so.FindProperty("authoredWeight").floatValue = 1f;
            so.FindProperty("mathWeight").floatValue = 0f;
            so.FindProperty("sequenceWeight").floatValue = 0f;
            so.FindProperty("wikipediaSources").arraySize = 0;

            so.ApplyModifiedPropertiesWithoutUndo();
            return flow;
        }

        private static void AddRewardBridge(GameObject root, QuizController quiz, QuizView view)
        {
            var bridge = root.AddComponent<QuizRewardBridge>();

            var so = new SerializedObject(bridge);
            so.FindProperty("quiz").objectReferenceValue = quiz;
            so.FindProperty("view").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The same split the fight uses, plus the placard that stands in for the
        /// quiz until it opens. Without the placard the quiz half is a blank
        /// rectangle for the first four steps, which reads as a bug.
        /// </summary>
        private static SplitScreenLayout BuildSplitScreen(
            QuizController quiz, UiSpriteFactory.Sprites sprites, TMP_FontAsset font, out GameObject placeholder)
        {
            var root = new GameObject(SplitRootName, typeof(Canvas), typeof(CanvasScaler));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Behind the quiz panel and the HUD, and with no raycaster, so a
            // full-height image over the quiz half cannot swallow answer clicks.
            canvas.sortingOrder = -100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = UiFactory.NewRect("Quiz Backdrop", root.transform);
            UiFactory.AddImage(backdrop, null, MenuTheme.Background);

            RectTransform seam = UiFactory.NewRect("Seam", root.transform);
            UiFactory.AddImage(seam, null, MenuTheme.Border);

            placeholder = BuildQuizPlaceholder(backdrop, sprites, font);

            var layout = root.AddComponent<SplitScreenLayout>();

            var so = new SerializedObject(layout);
            so.FindProperty("fighterCamera").objectReferenceValue = Object.FindAnyObjectByType<Camera>();
            so.FindProperty("quizBackdrop").objectReferenceValue = backdrop;
            so.FindProperty("seam").objectReferenceValue = seam;
            so.FindProperty("quizPanel").objectReferenceValue = FindQuizPanelContainer(quiz);
            so.ApplyModifiedPropertiesWithoutUndo();

            return layout;
        }

        private static GameObject BuildQuizPlaceholder(
            RectTransform backdrop, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.NewRect("Quiz Placeholder", backdrop));

            var centre = new Vector2(0.5f, 0.5f);
            var cardSize = new Vector2(380f, 300f);

            RectTransform holder = UiFactory.Place(UiFactory.NewRect("Card", root), centre, centre, Vector2.zero, cardSize);

            RectTransform shade = UiFactory.Stretch(UiFactory.NewRect("Shade", holder), -Elevation);
            UiFactory.AddImage(shade, sprites.CardGlow, MenuTheme.Shadow);

            RectTransform card = UiFactory.Stretch(UiFactory.NewRect("Face", holder));
            UiFactory.AddImage(card, sprites.Card, MenuTheme.Surface);

            // The same accent chip the coach card wears, so the two halves of the
            // screen look like they were designed by one person.
            RectTransform chip = UiFactory.Place(
                UiFactory.NewRect("Chip", card), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(150f, 34f));
            UiFactory.AddImage(chip, sprites.Pill, MenuTheme.AccentWash);

            TMP_Text chipLabel = UiFactory.AddLabel(chip, "Label", "UP NEXT", 18f, MenuTheme.AccentInk, font, FontStyles.Bold);
            UiFactory.Stretch(chipLabel.rectTransform);

            TMP_Text heading = UiFactory.AddLabel(card, "Heading", "Your mind", 36f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            UiFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(320f, 46f));

            TMP_Text body = UiFactory.AddLabel(
                card, "Body", "The questions open\nonce you have thrown\na few punches.", 22f, MenuTheme.TextSecondary, font);
            UiFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(320f, 110f));
            body.lineSpacing = 8f;

            return root.gameObject;
        }

        /// <summary>
        /// The coach card, parented inside the HUD's viewport rect. That is what
        /// keeps it inside the fight's half of the window: the split-screen
        /// layout anchors that rect, so the card follows the ratio without this
        /// builder ever naming it.
        /// </summary>
        private static TutorialCoach BuildCoach(RectTransform viewport, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            if (viewport == null)
            {
                Debug.LogError("No fighter HUD viewport to hang the tutorial card on, so the tutorial has nothing to say.");
                return null;
            }

            // Along the TOP of the fight, under the health bars, and not along
            // the bottom where a card this size belongs by instinct. The camera
            // holds the fighter around the middle of the view, so a card at the
            // bottom covers them from the knees to the chest -- on the one
            // screen whose whole job is showing you what your fighter does.
            RectTransform coachRoot = UiFactory.Place(
                UiFactory.NewRect("Coach", viewport),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -TopMargin),
                CardSize);

            // The shade goes down first so the card covers it. A white panel over
            // a lit 3D stage with nothing behind it reads as a hole cut in the
            // screen rather than as something resting on top of it.
            RectTransform shade = UiFactory.Stretch(UiFactory.NewRect("Shade", coachRoot), -Elevation);
            UiFactory.AddImage(shade, sprites.CardGlow, MenuTheme.Shadow);

            RectTransform card = UiFactory.Stretch(UiFactory.NewRect("Card", coachRoot));

            // Opaque, not the HUD's translucent white. The stage shows through a
            // 94%-alpha panel just enough to fight the type behind every letter.
            UiFactory.AddImage(card, sprites.Card, MenuTheme.Surface, raycast: true);

            TMP_Text counter = BuildCounterChip(card, sprites, font);

            TMP_Text title = UiFactory.AddLabel(
                card, "Title", "Title", 36f, MenuTheme.TextPrimary, font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(title.rectTransform, TopLeft, TopLeft, new Vector2(Pad, -62f), new Vector2(780f, 54f));

            TMP_Text body = UiFactory.AddLabel(
                card, "Body", "Body", 24f, MenuTheme.TextPrimary, font, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            UiFactory.Place(body.rectTransform, TopLeft, TopLeft, new Vector2(Pad, -124f), new Vector2(CardSize.x - (Pad * 2f), 84f));

            // The title gets the same net, one size band up. A single line at 36
            // needs 49px and the rect is 54, so there is room for one longer
            // heading before anything has to give.
            title.enableAutoSizing = true;
            title.fontSizeMin = 28f;
            title.fontSizeMax = 36f;

            // Auto-sizing is the safety net, not the plan: the copy is written to
            // fit at 24. It is here so that re-wording a step -- the thing most
            // likely to happen to this screen -- shrinks the text instead of
            // quietly running it off the bottom of the card.
            body.enableAutoSizing = true;
            body.fontSizeMin = 19f;
            body.fontSizeMax = 24f;
            body.lineSpacing = 6f;

            TMP_Text objective = BuildObjectiveChip(card, sprites, font, out GameObject objectiveRoot, out RectTransform fill);

            Button skip = UiFactory.MakePill(
                card, "Button_Skip", "SKIP TUTORIAL", SkipSize, sprites, font, 17f, UiFactory.PillStyle.Subdued, out _);
            UiFactory.Place((RectTransform)skip.transform, TopRight, TopRight, new Vector2(-Pad, -22f), SkipSize);

            // The one filled button on the screen. Everything else here is a
            // quiet card, so this is what the eye lands on.
            Button primary = UiFactory.MakePill(
                card, "Button_Primary", "CONTINUE", ButtonSize, sprites, font, 23f, UiFactory.PillStyle.Primary, out TMP_Text primaryLabel);
            UiFactory.Place((RectTransform)primary.transform, BottomRight, BottomRight, new Vector2(-Pad, 26f), ButtonSize);

            Button secondary = UiFactory.MakePill(
                card, "Button_Secondary", "MAIN MENU", ButtonSize, sprites, font, 21f, UiFactory.PillStyle.Quiet, out TMP_Text secondaryLabel);
            UiFactory.Place(
                (RectTransform)secondary.transform, BottomRight, BottomRight, new Vector2(-(Pad + ButtonSize.x + 16f), 26f), ButtonSize);

            var coach = coachRoot.gameObject.AddComponent<TutorialCoach>();

            var so = new SerializedObject(coach);
            so.FindProperty("counterLabel").objectReferenceValue = counter;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("bodyLabel").objectReferenceValue = body;
            so.FindProperty("objectiveLabel").objectReferenceValue = objective;
            so.FindProperty("objectiveRoot").objectReferenceValue = objectiveRoot;
            so.FindProperty("progressRoot").objectReferenceValue = fill.gameObject;
            so.FindProperty("progressFill").objectReferenceValue = fill;
            so.FindProperty("primaryButton").objectReferenceValue = primary;
            so.FindProperty("primaryLabel").objectReferenceValue = primaryLabel;
            so.FindProperty("secondaryButton").objectReferenceValue = secondary;
            so.FindProperty("secondaryLabel").objectReferenceValue = secondaryLabel;
            so.FindProperty("skipButton").objectReferenceValue = skip;
            so.ApplyModifiedPropertiesWithoutUndo();

            return coach;
        }

        /// <summary>
        /// "STEP 3 OF 10" as a tinted chip rather than grey text in a corner.
        /// It is the only thing on the card that says how long this is going to
        /// take, which is worth more than a caption's worth of emphasis.
        /// </summary>
        private static TMP_Text BuildCounterChip(RectTransform card, UiSpriteFactory.Sprites sprites, TMP_FontAsset font)
        {
            RectTransform chip = UiFactory.Place(
                UiFactory.NewRect("Counter", card), TopLeft, TopLeft, new Vector2(Pad, -20f), new Vector2(200f, 32f));
            UiFactory.AddImage(chip, sprites.Pill, MenuTheme.AccentWash);

            TMP_Text label = UiFactory.AddLabel(
                chip, "Label", "STEP 1 OF 1", 18f, MenuTheme.AccentInk, font, FontStyles.Bold);
            UiFactory.Stretch(label.rectTransform);

            return label;
        }

        /// <summary>
        /// The objective, in a pill that fills up as the player gets closer to
        /// finishing it.
        ///
        /// One element instead of two. A separate caption and progress bar was
        /// the obvious layout and it cost a whole row of a card that has none to
        /// spare -- and it split "what you have to do" from "how far in you are"
        /// into two things to look at.
        /// </summary>
        private static TMP_Text BuildObjectiveChip(
            RectTransform card, UiSpriteFactory.Sprites sprites, TMP_FontAsset font, out GameObject root, out RectTransform fill)
        {
            RectTransform chip = UiFactory.Place(
                UiFactory.NewRect("Objective", card), BottomLeft, BottomLeft, new Vector2(Pad, 32f), new Vector2(600f, 44f));
            UiFactory.AddImage(chip, sprites.Pill, MenuTheme.Track);

            // Before the label in the child order, so the text stays on top of
            // the fill sweeping under it.
            fill = UiFactory.NewRect("Fill", chip);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            UiFactory.AddImage(fill, sprites.Pill, MenuTheme.AccentWash);

            TMP_Text label = UiFactory.AddLabel(
                chip, "Label", "Objective", 21f, MenuTheme.AccentInk, font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(label.rectTransform, BottomLeft, BottomLeft, new Vector2(22f, 0f), new Vector2(556f, 44f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 21f;

            root = chip.gameObject;
            return label;
        }

        /// <summary>
        /// The director, the fade and the menu's button sounds. All three go on
        /// one root so the tutorial's own machinery is one object in the
        /// hierarchy rather than three.
        /// </summary>
        private static void BuildDirector(
            TutorialCoach coach,
            GameObject player,
            GameObject dummy,
            QuizController quiz,
            QuizFlow flow,
            GameObject panel,
            GameObject placeholder,
            SplitScreenLayout layout)
        {
            var root = new GameObject(TutorialRootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above the HUD and the quiz: this canvas only carries the fade,
            // which has to cover both halves.
            canvas.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            MenuUiBuilder.AddFade((RectTransform)root.transform, root);
            MenuUiBuilder.AddSounds(root);

            var director = root.AddComponent<TutorialDirector>();

            var so = new SerializedObject(director);
            so.FindProperty("coach").objectReferenceValue = coach;
            so.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
            so.FindProperty("input").objectReferenceValue = player.GetComponent<PlayerInputReader>();
            so.FindProperty("attack").objectReferenceValue = player.GetComponent<PlayerAttack>();
            so.FindProperty("resources").objectReferenceValue = player.GetComponent<FighterResources>();
            so.FindProperty("quizPanel").objectReferenceValue = panel;
            so.FindProperty("quizPlaceholder").objectReferenceValue = placeholder;
            so.FindProperty("quiz").objectReferenceValue = quiz;
            so.FindProperty("quizView").objectReferenceValue = FindQuizView(quiz);
            so.FindProperty("quizFlow").objectReferenceValue = flow;
            so.FindProperty("layout").objectReferenceValue = layout;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (dummy == null)
            {
                Debug.LogWarning("The tutorial has no trainer to punch.", root);
            }
        }

        /// <summary>
        /// The panel is already switched off by the time this runs, so the
        /// search has to include inactive children. Without that the director
        /// falls back to its own copy of the fast-solve threshold, and the
        /// tutorial could disagree with the bar the player is watching.
        /// </summary>
        private static QuizView FindQuizView(QuizController quiz)
        {
            if (quiz == null)
            {
                return null;
            }

            return quiz.GetComponentInChildren<QuizView>(includeInactive: true);
        }

        private static RectTransform FindHudViewport()
        {
            var hud = Object.FindAnyObjectByType<ThinkFast.UI.FighterHud>();
            if (hud == null)
            {
                return null;
            }

            return hud.transform.Find("Viewport") as RectTransform;
        }

        private static RectTransform FindQuizPanelContainer(QuizController quiz)
        {
            if (quiz == null)
            {
                return null;
            }

            Transform container = quiz.transform.Find(PanelContainerName);
            if (container == null)
            {
                Debug.LogWarning($"No '{PanelContainerName}' under the quiz panel, so the panel will not be moved into its half.", quiz);
                return null;
            }

            return container as RectTransform;
        }

        private static void FrameCamera(Transform player)
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(0f, 3f, -9f);
            camera.transform.rotation = Quaternion.identity;

            var follow = camera.GetComponent<FollowCamera>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<FollowCamera>();
            }

            SetObjectField(follow, "target", player);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            // Both the answer buttons and the coach card's buttons are clicked
            // with the mouse. No object is selected on load, deliberately: a
            // selected button would be re-pressed by the punch key, which the UI
            // module also treats as submit.
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.name = "EventSystem";
        }

        /// <summary>
        /// Adds the tutorial to Build Settings, which is what makes the menu's
        /// card able to load it at all. Done here rather than by hand because the
        /// scene is generated and has no GUID until it has been saved once.
        /// </summary>
        private static void EnsureInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene entry in scenes)
            {
                if (entry.path == ScenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObjectField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"'{target.GetType().Name}' has no serialized field '{field}'.", target);
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
