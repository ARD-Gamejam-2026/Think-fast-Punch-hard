using ThinkFast.Rounds;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.RoundsEditor
{
    /// <summary>
    /// Closes the game loop: menu to fight, fight to end screen, end screen back
    /// to the menu.
    ///
    /// Touches two scenes, because that is what a transition is -- the fight
    /// needs something to run it, and the end screen needs to be told which
    /// ending to show. Both halves are generated so neither is a hand-made scene
    /// edit nobody can review.
    /// </summary>
    public static class RoundFlowBuilder
    {
        private const string FightScenePath = "Assets/Scenes/PlayerControllerTest.unity";
        private const string EndScenePath = "Assets/Scenes/Scene_End.unity";

        private const string RoundFlowRootName = "--- Round Flow (generated) ---";
        private const string EndScreenRootName = "--- End Screen (generated) ---";

        /// <summary>The label the end screen writes its outcome into, as authored in the scene.</summary>
        private const string EndMessageName = "TMP_EndMessage";

        [MenuItem("Tools/Think Fast/Build Round Flow")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildEndScreen();
            BuildFightSide();

            Debug.Log("Built the round flow. A knockout now records the outcome and loads the end screen, which says which ending it was and returns to the menu.");
        }

        /// <summary>
        /// Adds the component that writes the outcome into the end screen's
        /// existing label. The scene's layout is left exactly as authored -- only
        /// the text is decided at runtime.
        /// </summary>
        private static void BuildEndScreen()
        {
            Scene scene = EditorSceneManager.OpenScene(EndScenePath, OpenSceneMode.Single);
            RemoveExisting(scene, EndScreenRootName);

            var root = new GameObject(EndScreenRootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Round Flow");

            var endScreen = root.AddComponent<EndScreen>();
            TMP_Text label = FindEndMessage();

            var so = new SerializedObject(endScreen);
            so.FindProperty("message").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (label == null)
            {
                Debug.LogWarning($"No '{EndMessageName}' in {EndScenePath}, so the end screen will keep its authored text whichever way the fight went.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Adds the round flow to the fight scene and stands down the debug
        /// banner it replaces.
        /// </summary>
        private static void BuildFightSide()
        {
            Scene scene = EditorSceneManager.OpenScene(FightScenePath, OpenSceneMode.Single);
            RemoveExisting(scene, RoundFlowRootName);

            var root = new GameObject(RoundFlowRootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Round Flow");
            root.AddComponent<RoundFlow>();

            StandDownDebugBanner();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Finds the authored end-screen label by name, falling back to the only
        /// other thing it could be. The scene has a second label on the Return
        /// button, so the type alone is not enough to pick the right one.
        /// </summary>
        private static TMP_Text FindEndMessage()
        {
            foreach (TMP_Text candidate in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if (candidate.name == EndMessageName)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Switches off the throwaway banner. Its job -- telling the player the
        /// fight is over -- now belongs to the end screen, and its "press Enter
        /// to fight again" prompt would be a lie next to a transition that
        /// happens on its own.
        /// </summary>
        private static void StandDownDebugBanner()
        {
            var banner = Object.FindAnyObjectByType<DebugRoundBanner>();
            if (banner == null || !banner.enabled)
            {
                return;
            }

            banner.enabled = false;
            EditorUtility.SetDirty(banner);
            Debug.Log("Disabled DebugRoundBanner: the end screen replaces it. Re-tick it on the Round Debug object to get the instant Enter restart back.", banner);
        }

        private static void RemoveExisting(Scene scene, string rootName)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == rootName)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }
}
