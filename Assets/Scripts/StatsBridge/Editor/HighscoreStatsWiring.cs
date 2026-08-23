using ThinkFast.Combat;
using ThinkFast.Enemy;
using ThinkFast.Player;
using ThinkFast.Quiz;
using ThinkFast.Rounds;
using ThinkFast.Stats;
using ThinkFast.UI;
using ThinkFast.UIEditor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.StatsEditor
{
    /// <summary>
    /// Wires the highscore-stats components into the fight, end and menu scenes,
    /// so the match statistics are collected and uploaded without hand-placing
    /// each component. Safe to re-run: it updates what it already added rather
    /// than duplicating it.
    /// </summary>
    public static class HighscoreStatsWiring
    {
        private const string FightScene = "Assets/Scenes/PlayerControllerTest.unity";
        private const string EndScene = "Assets/Scenes/Scene_End.unity";
        private const string MenuScene = "Assets/Scenes/Scene_Menu.unity";
        private const string PromptName = "Highscore Name Prompt";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        /// <summary>Wires every scene the highscore stats need.</summary>
        [MenuItem("Tools/Think Fast/Wire Highscore Stats")]
        public static void WireHighscoreStats()
        {
            WireScene(FightScene, WireFightScene);
            WireScene(EndScene, WireEndScene);
            WireScene(MenuScene, WireMenuScene);
            Debug.Log("[HighscoreStatsWiring] Done. Review the scenes and re-save if needed.");
        }

        // Opens the scene, runs the wiring action, then saves it. A missing scene
        // is reported and skipped rather than aborting the whole command.
        private static void WireScene(string path, System.Action wire)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning("[HighscoreStatsWiring] Scene not found, skipped: " + path);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            wire();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HighscoreStatsWiring] Wired " + path);
        }

        // A FighterStatsReporter on every fighter's Health, and one coordinator.
        private static void WireFightScene()
        {
            Health[] healths = Object.FindObjectsByType<Health>(FindObjectsInactive.Include);
            foreach (Health health in healths)
            {
                WireReporter(health);
            }

            WireCoordinator();
        }

        // Adds (or updates) the reporter on this Health, choosing its role from
        // whether the fighter is the player or the enemy.
        private static void WireReporter(Health health)
        {
            bool isPlayer = HasInHierarchy<PlayerController>(health);
            bool isEnemy = HasInHierarchy<EnemyMotor>(health);
            if (!isPlayer && !isEnemy)
            {
                return;
            }

            FighterRole role = FighterRole.Opponent;
            if (isPlayer)
            {
                role = FighterRole.Player;
            }

            FighterStatsReporter reporter = EnsureComponent<FighterStatsReporter>(health.gameObject);
            var serialized = new SerializedObject(reporter);
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.FindProperty("health").objectReferenceValue = health;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // One MatchStatsCoordinator, placed on the QuizController's object so it
        // sits with the rest of the quiz wiring. It auto-finds the controller.
        private static void WireCoordinator()
        {
            QuizController quiz = Object.FindAnyObjectByType<QuizController>(FindObjectsInactive.Include);
            if (quiz == null)
            {
                Debug.LogWarning("[HighscoreStatsWiring] No QuizController in the fight scene; coordinator not added.");
                return;
            }

            EnsureComponent<MatchStatsCoordinator>(quiz.gameObject);
        }

        // The uploader on the end screen plus the name prompt it shows when no
        // name was set on the menu. Its database URL and key are component
        // defaults, so nothing else needs setting here.
        private static void WireEndScene()
        {
            EndScreen endScreen = Object.FindAnyObjectByType<EndScreen>(FindObjectsInactive.Include);
            if (endScreen == null)
            {
                Debug.LogWarning("[HighscoreStatsWiring] No EndScreen in the end scene; uploader not added.");
                return;
            }

            EndScreenUploader uploader = EnsureComponent<EndScreenUploader>(endScreen.gameObject);

            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[HighscoreStatsWiring] No Canvas in the end scene; name prompt not built.");
                return;
            }

            BuildNamePrompt(canvas.transform, out GameObject panel, out TMP_InputField field, out Button save);
            AssignPrompt(uploader, panel, field, save);
        }

        // Builds the hidden name-prompt card (title + input + save button) under
        // the end-screen canvas, rebuilding any earlier one so re-runs are clean.
        private static void BuildNamePrompt(Transform canvas, out GameObject panel, out TMP_InputField field, out Button save)
        {
            Transform existing = canvas.Find(PromptName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            RectTransform root = UiFactory.NewRect(PromptName, canvas);
            UiFactory.Place(root, Center, Center, Vector2.zero, new Vector2(720f, 400f));
            UiFactory.AddImage(root, null, new Color(0f, 0f, 0f, 0.6f), raycast: true);

            RectTransform card = UiFactory.NewRect("Card", root);
            UiFactory.Place(card, Center, Center, Vector2.zero, new Vector2(560f, 280f));
            UiFactory.AddImage(card, null, MenuTheme.Surface, raycast: true);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            TMP_Text title = UiFactory.AddLabel(card, "Title", "Enter your name", 40f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            UiFactory.Place(title.rectTransform, Center, Center, new Vector2(0f, 90f), new Vector2(500f, 60f));

            field = BuildField(card);
            save = BuildSaveButton(card, font);

            panel = root.gameObject;
            panel.SetActive(false);
        }

        private static TMP_InputField BuildField(Transform card)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "Name Field";
            go.transform.SetParent(card, false);
            UiFactory.Place(go.GetComponent<RectTransform>(), Center, Center, new Vector2(0f, 0f), new Vector2(460f, 70f));

            TMP_InputField input = go.GetComponent<TMP_InputField>();
            input.text = string.Empty;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "Your name";
            }

            return input;
        }

        private static Button BuildSaveButton(Transform card, TMP_FontAsset font)
        {
            RectTransform rect = UiFactory.NewRect("Save Button", card);
            UiFactory.Place(rect, Center, Center, new Vector2(0f, -95f), new Vector2(260f, 70f));
            Image image = UiFactory.AddImage(rect, null, MenuTheme.Accent, raycast: true);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = UiFactory.AddLabel(rect, "Label", "Save score", 30f, Color.white, font, FontStyles.Bold);
            UiFactory.Stretch(label.rectTransform);
            return button;
        }

        private static void AssignPrompt(EndScreenUploader uploader, GameObject panel, TMP_InputField field, Button save)
        {
            var serialized = new SerializedObject(uploader);
            serialized.FindProperty("namePrompt").objectReferenceValue = panel;
            serialized.FindProperty("nameField").objectReferenceValue = field;
            serialized.FindProperty("saveButton").objectReferenceValue = save;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // The name field, only if the menu already has a text input to bind to.
        private static void WireMenuScene()
        {
            TMP_InputField field = Object.FindAnyObjectByType<TMP_InputField>(FindObjectsInactive.Include);
            if (field == null)
            {
                Debug.Log("[HighscoreStatsWiring] No TMP_InputField in the menu; PlayerNameField skipped (names default to 'anon').");
                return;
            }

            PlayerNameField nameField = EnsureComponent<PlayerNameField>(field.gameObject);
            var serialized = new SerializedObject(nameField);
            serialized.FindProperty("field").objectReferenceValue = field;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasInHierarchy<T>(Component from) where T : Component
        {
            if (from.GetComponentInParent<T>(true) != null)
            {
                return true;
            }

            return from.GetComponentInChildren<T>(true) != null;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return target.AddComponent<T>();
        }
    }
}
