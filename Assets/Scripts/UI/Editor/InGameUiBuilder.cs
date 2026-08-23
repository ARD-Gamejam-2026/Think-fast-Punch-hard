using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Runs the whole in-game UI pass in the order the pieces depend on each
    /// other.
    ///
    /// The order is the point. The split-screen builder instantiates the quiz
    /// panel prefab, so the prefab has to be restyled *before* it, or the fight
    /// scene gets a fresh copy of the old look. The HUD is built last because it
    /// goes into whichever scene is open, and the split-screen builder is what
    /// opens it.
    /// </summary>
    public static class InGameUiBuilder
    {
        private const string FightScenePath = "Assets/Scenes/PlayerControllerTest.unity";

        [MenuItem("Tools/Think Fast/Build In-Game UI")]
        public static void Build()
        {
            // Skipped in batch mode, where there is nobody to ask and the prompt
            // is what a headless run would hang on.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // Shapes first: everything below asks for them by name.
            UiSpriteFactory.BuildAll();

            QuizPanelRestyler.Restyle();

            // Opens the fight scene, rebuilds the quiz half from the restyled
            // prefab, and saves.
            SplitScreenFightBuilder.Build();

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FightScenePath)
            {
                Debug.LogWarning($"Expected {FightScenePath} to be open after the split-screen build, but {scene.path} is. The HUD was not rebuilt.");
                return;
            }

            FighterHudBuilder.Build();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Rebuilt the in-game UI: quiz panel, split screen and fighter HUD, all in the menu's palette.");
        }
    }
}
