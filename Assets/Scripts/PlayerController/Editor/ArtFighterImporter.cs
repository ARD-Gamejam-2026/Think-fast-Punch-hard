using ThinkFast.CameraRig;
using ThinkFast.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.PlayerEditor
{
    /// <summary>
    /// Brings the animated Player and Enemy out of the art scene and into the
    /// scene the game actually plays.
    ///
    /// The art scene is a copy of the test rig with the placeholder capsules
    /// replaced by the fighter model prefab, and with the attack frame data
    /// retimed to match the animations. Both of those live on the fighters
    /// themselves, so the fighters are what has to move -- rebuilding them from
    /// the rig builder would throw the retiming away and put the capsules back.
    ///
    /// The move is a real reparent rather than a copy, so the nested model prefab,
    /// its animator, and every tuned value come across exactly as authored. The
    /// art scene is closed without saving, so it keeps its own copy.
    /// </summary>
    public static class ArtFighterImporter
    {
        private const string FightScenePath = "Assets/Scenes/PlayerControllerTest.unity";
        private const string ArtScenePath = "Assets/Scenes/Art.unity";

        private const string RigRootName = "--- Test Rig (generated) ---";

        [MenuItem("Tools/Think Fast/Import Fighters From Art Scene")]
        public static void Import()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene fight = EditorSceneManager.OpenScene(FightScenePath, OpenSceneMode.Single);
            Scene art = EditorSceneManager.OpenScene(ArtScenePath, OpenSceneMode.Additive);

            Transform rigRoot = FindRigRoot(fight);
            if (rigRoot == null)
            {
                Debug.LogError($"No '{RigRootName}' in {FightScenePath}. Build the test scene first.");
                EditorSceneManager.CloseScene(art, true);
                return;
            }

            GameObject player = Adopt(art, fight, rigRoot, "Player");
            GameObject enemy = Adopt(art, fight, rigRoot, "Enemy");

            EditorSceneManager.CloseScene(art, removeScene: true);

            if (player == null || enemy == null)
            {
                Debug.LogError("Could not find both fighters in the art scene; nothing was changed.");
                return;
            }

            Rewire(player, enemy);

            EditorSceneManager.MarkSceneDirty(fight);
            EditorSceneManager.SaveScene(fight);

            Debug.Log("Imported the animated fighters from the art scene. Re-running 'Build PlayerController Test Scene' will replace them with the placeholder capsules again -- re-run this afterwards if you do.");
        }

        private static Transform FindRigRoot(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == RigRootName)
                {
                    return go.transform;
                }
            }

            return null;
        }

        /// <summary>
        /// Moves one fighter from the art scene into the fight scene's rig,
        /// removing whichever fighter was there before.
        ///
        /// The old one is destroyed only once the new one has been found, so a
        /// missing fighter in the art scene leaves the scene as it was rather than
        /// stripping it.
        /// </summary>
        private static GameObject Adopt(Scene art, Scene fight, Transform rigRoot, string name)
        {
            GameObject incoming = FindInRig(art, name);
            if (incoming == null)
            {
                Debug.LogError($"No '{name}' under the rig root in {ArtScenePath}.");
                return null;
            }

            // Position is read before the move: the two scenes were built by the
            // same generator so they should agree, but the fight scene's spawn is
            // the one the stage was tuned around, and the knockouts record it as
            // the point a fighter is revived to.
            GameObject outgoing = FindInRig(fight, name);
            Vector3 spawn = incoming.transform.position;

            if (outgoing != null)
            {
                if (outgoing.transform.position != spawn)
                {
                    Debug.LogWarning($"'{name}' sits at {outgoing.transform.position} in the fight scene but {spawn} in the art scene. Keeping the fight scene's position.");
                    spawn = outgoing.transform.position;
                }

                Object.DestroyImmediate(outgoing);
            }

            // Only root objects can change scene, so it is detached first and
            // re-parented into the rig afterwards.
            incoming.transform.SetParent(null, worldPositionStays: true);
            SceneManager.MoveGameObjectToScene(incoming, fight);
            incoming.transform.SetParent(rigRoot, worldPositionStays: true);
            incoming.transform.position = spawn;

            return incoming;
        }

        private static GameObject FindInRig(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
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

        /// <summary>
        /// Re-points the two things that held a direct reference to the fighters
        /// that were just replaced: the camera, and the opponent's idea of what it
        /// is chasing. Everything else in the scene finds its fighter at runtime.
        /// </summary>
        private static void Rewire(GameObject player, GameObject enemy)
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("No FollowCamera in the scene, so nothing is following the new player.");
            }

            var brain = enemy.GetComponent<EnemyBrain>();
            if (brain == null)
            {
                Debug.LogWarning("The imported enemy has no EnemyBrain, so it will not chase anything.");
                return;
            }

            var brainSo = new SerializedObject(brain);
            brainSo.FindProperty("target").objectReferenceValue = player.transform;
            brainSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
