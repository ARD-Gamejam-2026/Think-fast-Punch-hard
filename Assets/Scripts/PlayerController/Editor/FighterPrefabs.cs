using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThinkFast.PlayerEditor
{
    /// <summary>
    /// Turns the fighters in the open scene into prefabs, and hands them back to
    /// whoever needs to spawn one.
    ///
    /// This is what ends the rebuild dance. The fighters used to be generated from
    /// code, so rebuilding the test scene threw away the models and the
    /// animation-matched tuning and put grey capsules back, and the art had to be
    /// re-imported every time. With prefabs the fighter *is* the asset: the scene
    /// builder instantiates it, and rebuilding the stage no longer touches it.
    ///
    /// Save again whenever the fighters themselves change -- new animation, new
    /// tuning -- and every scene that instantiates them picks it up.
    /// </summary>
    public static class FighterPrefabs
    {
        public const string PlayerPath = "Assets/Prefabs/Player.prefab";
        public const string EnemyPath = "Assets/Prefabs/Enemy.prefab";

        private const string RigRootName = "--- Test Rig (generated) ---";
        private const string Folder = "Assets/Prefabs";

        [MenuItem("Tools/Think Fast/Save Fighters As Prefabs")]
        public static void Save()
        {
            GameObject player = FindInRig("Player");
            GameObject enemy = FindInRig("Enemy");

            if (player == null || enemy == null)
            {
                Debug.LogError($"Both fighters must be in the open scene under '{RigRootName}'. Open the fight scene first.");
                return;
            }

            EnsureFolder();

            SaveOne(player, PlayerPath);
            SaveOne(enemy, EnemyPath);

            AssetDatabase.SaveAssets();
            Debug.Log($"Saved the fighters to {PlayerPath} and {EnemyPath}. Rebuilding the test scene will now instantiate these instead of generating capsules.");
        }

        /// <summary>
        /// Loads a fighter prefab, or null when it has not been saved yet. Callers
        /// fall back to generating one, so a fresh clone of the repo still builds a
        /// playable scene.
        /// </summary>
        public static GameObject Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>
        /// Saves one fighter, replacing the scene object with an instance of the
        /// prefab it just became.
        ///
        /// Connecting the scene object is the point of doing it this way: a
        /// disconnected copy would drift from the asset the moment either was
        /// edited, and the scene would quietly stop matching what every other
        /// scene spawns.
        /// </summary>
        private static void SaveOne(GameObject fighter, string path)
        {
            // Scene-only references cannot be stored in an asset. The opponent's
            // target is the one that matters, and the scene builder re-assigns it
            // after spawning, so clearing it here keeps the prefab honest rather
            // than carrying a reference that silently resolves to nothing.
            ClearSceneReferences(fighter);

            PrefabUtility.SaveAsPrefabAssetAndConnect(fighter, path, InteractionMode.AutomatedAction);
        }

        private static void ClearSceneReferences(GameObject fighter)
        {
            var brain = fighter.GetComponent<ThinkFast.Enemy.EnemyBrain>();
            if (brain == null)
            {
                return;
            }

            var so = new SerializedObject(brain);
            so.FindProperty("target").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindInRig(string name)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != RigRootName)
                {
                    continue;
                }

                Transform found = root.transform.Find(name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                return;
            }

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }
    }
}
