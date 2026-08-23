using ThinkFast.Combat;
using ThinkFast.Enemy;
using ThinkFast.Player;
using ThinkFast.Quiz;
using ThinkFast.Rounds;
using ThinkFast.Stats;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private const string FightScene = "Assets/Scenes/SampleScene.unity";
        private const string EndScene = "Assets/Scenes/Scene_End.unity";
        private const string MenuScene = "Assets/Scenes/Scene_Menu.unity";

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
            Health[] healths = Object.FindObjectsByType<Health>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        // The uploader on the end screen. Its database URL and key are already
        // component defaults, so nothing else needs setting here.
        private static void WireEndScene()
        {
            EndScreen endScreen = Object.FindAnyObjectByType<EndScreen>(FindObjectsInactive.Include);
            if (endScreen == null)
            {
                Debug.LogWarning("[HighscoreStatsWiring] No EndScreen in the end scene; uploader not added.");
                return;
            }

            EnsureComponent<EndScreenUploader>(endScreen.gameObject);
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
