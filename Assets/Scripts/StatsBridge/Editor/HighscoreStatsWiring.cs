using System.Collections.Generic;
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
        private const string FieldName = "Highscore Name Field";
        private const string OldPromptName = "Highscore Name Prompt";

        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);

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

        // The uploader on the end screen plus an inline name field below the
        // content, which the player reviews or edits; leaving the screen (Fight
        // Again / Menu) confirms and uploads. URL and key are component defaults.
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
                Debug.LogWarning("[HighscoreStatsWiring] No Canvas in the end scene; name field not built.");
                return;
            }

            DestroyExistingFields();
            TMP_InputField field = BuildLabeledField(canvas.transform, 90f);
            var serialized = new SerializedObject(uploader);
            serialized.FindProperty("nameField").objectReferenceValue = field;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Removes every earlier name field and the old modal prompt anywhere in
        // the scene, not just under one canvas -- these screens have more than
        // one canvas, so a per-canvas search left duplicates behind.
        private static void DestroyExistingFields()
        {
            var doomed = new List<GameObject>();
            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (transform != null && (transform.name == FieldName || transform.name == OldPromptName))
                {
                    doomed.Add(transform.gameObject);
                }
            }

            foreach (GameObject go in doomed)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        // Builds a labelled "Name" field anchored to the bottom-centre of the
        // canvas, below the screen's content.
        private static TMP_InputField BuildLabeledField(Transform canvas, float bottomOffset)
        {
            RectTransform root = UiFactory.NewRect(FieldName, canvas);
            UiFactory.Place(root, BottomCenter, BottomCenter, new Vector2(0f, bottomOffset), new Vector2(560f, 150f));

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            TMP_Text label = UiFactory.AddLabel(root, "Label", "Name", 30f, MenuTheme.TextPrimary, font, FontStyles.Bold);
            UiFactory.Place(label.rectTransform, TopCenter, TopCenter, new Vector2(0f, 0f), new Vector2(560f, 36f));

            TMP_InputField field = CreateInput(root, "Field", "Your name");
            UiFactory.Place(field.GetComponent<RectTransform>(), TopCenter, TopCenter, new Vector2(0f, -46f), new Vector2(480f, 88f));
            return field;
        }

        // Creates a TMP input field with a placeholder, parented and named. Uses
        // the TMP default control so the field, viewport and caret are complete.
        private static TMP_InputField CreateInput(Transform parent, string name, string placeholder)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);

            TMP_InputField input = go.GetComponent<TMP_InputField>();
            input.text = string.Empty;
            input.pointSize = 40f;
            if (input.textComponent != null)
            {
                input.textComponent.fontSize = 40f;
                input.textComponent.alignment = TextAlignmentOptions.Center;
            }

            if (input.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = placeholder;
                placeholderText.fontSize = 40f;
                placeholderText.alignment = TextAlignmentOptions.Center;
            }

            return input;
        }

        // A dedicated name field on the menu, bound to PlayerName. It is built
        // here rather than reusing whatever TMP_InputField happens to exist so it
        // never binds to an unrelated field (e.g. the debug console input).
        private static void WireMenuScene()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[HighscoreStatsWiring] No Canvas in the menu; name field not built.");
                return;
            }

            // Drop any earlier binding (including a stray one on the debug field)
            // and every earlier field, so re-runs never accumulate duplicates.
            foreach (PlayerNameField stale in Object.FindObjectsByType<PlayerNameField>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(stale);
            }

            DestroyExistingFields();
            TMP_InputField field = BuildLabeledField(canvas.transform, 120f);
            PlayerNameField binder = field.gameObject.AddComponent<PlayerNameField>();
            var serialized = new SerializedObject(binder);
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
