using ThinkFast.Enemy;
using ThinkFast.Quiz;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.RoundsEditor
{
    /// <summary>
    /// One-click scene wiring for the difficulty ramp (issue #27). Adds a
    /// <see cref="DifficultyRamp"/> beside the open scene's
    /// <see cref="QuizController"/> and an <see cref="EnemyDifficultySpeed"/>
    /// beside its <see cref="EnemyMotor"/>, which is all the feature needs to run:
    /// both components self-resolve their dependencies, so there are no fields to
    /// wire.
    ///
    /// It lives in Assembly-CSharp-Editor (this Editor folder has no asmdef) on
    /// purpose -- it is the only editor assembly that can see both halves at once:
    /// <see cref="EnemyDifficultySpeed"/>/<see cref="EnemyMotor"/> in Assembly-CSharp
    /// and <see cref="DifficultyRamp"/>/<see cref="QuizController"/> in the
    /// auto-referenced Quiz assembly. The Quiz.Editor asmdef could not, because it
    /// cannot reference Assembly-CSharp.
    ///
    /// Idempotent: run it twice and the second run adds nothing. It marks the
    /// active scene dirty but does not save it, so the wiring can be reviewed
    /// before it is committed.
    /// </summary>
    public static class DifficultyRampSetup
    {
        [MenuItem("Tools/Think Fast/Add Difficulty Ramp To Scene")]
        public static void AddToScene()
        {
            int added = 0;
            added += EnsureComponent<QuizController, DifficultyRamp>("DifficultyRamp", "QuizController");
            added += EnsureComponent<EnemyMotor, EnemyDifficultySpeed>("EnemyDifficultySpeed", "EnemyMotor");

            if (added == 0)
            {
                Debug.Log(
                    "Difficulty ramp: nothing to add -- it is already wired, or the open scene "
                    + "has no QuizController / EnemyMotor. Open the fight+quiz scene and run this again.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                $"Difficulty ramp: added {added} component(s). Save the scene '{scene.name}' to keep the wiring.");
        }

        /// <summary>
        /// Adds <typeparamref name="TAdd"/> to every object in the open scene that
        /// has a <typeparamref name="THost"/> but not already the component, via an
        /// undoable AddComponent. Returns how many it added. Warns (and adds
        /// nothing) when no host exists, so a wrong open scene is not silent.
        /// </summary>
        private static int EnsureComponent<THost, TAdd>(string addName, string hostName)
            where THost : Component
            where TAdd : Component
        {
            THost[] hosts = Object.FindObjectsByType<THost>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (hosts.Length == 0)
            {
                Debug.LogWarning(
                    $"No {hostName} in the open scene, so no {addName} was added.");
                return 0;
            }

            int added = 0;
            foreach (THost host in hosts)
            {
                if (host.GetComponent<TAdd>() != null)
                {
                    continue;
                }

                Undo.AddComponent<TAdd>(host.gameObject);
                Debug.Log($"Added {addName} to '{host.gameObject.name}'.", host.gameObject);
                added++;
            }

            return added;
        }
    }
}
