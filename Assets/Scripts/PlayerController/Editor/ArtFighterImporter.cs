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

        /// <summary>
        /// The air swing's phases, redistributed without changing how long the
        /// swing lasts.
        ///
        /// The animation retimed the air attack to open its hitbox halfway through
        /// the clip, where the fist visually extends. That is right for a punch
        /// thrown standing still and wrong for a dive: the fighter is travelling
        /// through the whole startup, so by the time the hitbox opened it had
        /// already arced past what it aimed at, and air attacks simply stopped
        /// connecting.
        ///
        /// These keep the total at the animation's 0.36s -- so the clip still fits
        /// and nothing looks rushed -- and spend it differently: open earlier, and
        /// stay open across the whole visual strike. A hitbox that leads the fist
        /// slightly is far less noticeable than an opponent that never lands a
        /// dive. The README used the same trick once before, widening active from
        /// 0.07 to 0.10 for this exact reason.
        /// </summary>
        private const float AirStartup = 0.10f;

        private const float AirActive = 0.16f;

        private const float AirRecovery = 0.10f;

        /// <summary>
        /// How far the air hitbox sits below the fighter's centre.
        ///
        /// The animated fighters came in at -1, against -0.1 before. With a hitbox
        /// 1.1 tall, -1 puts it entirely underneath the fighter -- spanning -1.55
        /// to -0.45 -- so nothing at the diving fighter's own height can be hit at
        /// all, which is precisely where a player camping a platform edge stands.
        /// The brain still reads them as in range (attackVerticalRange is 1.3) and
        /// swings into empty air, and the fall during startup drops the box
        /// further still.
        ///
        /// -0.1 keeps the box centred on the fighter, covering its own height.
        /// </summary>
        private const float AirHitboxOffsetY = -0.1f;

        /// <summary>
        /// How much of its own movement the fighter keeps during the swing.
        ///
        /// Also reverted by the retiming, 0.9 to 0.6. The README is explicit about
        /// this one: a dive that brakes itself lands short of what it was aimed at,
        /// and the aiming already accounts for the braking, so slowing the fighter
        /// mid-swing makes it undershoot a target it had correctly predicted.
        /// </summary>
        private const float AirMoveControlScale = 0.9f;

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

            RebalanceAirAttack(player.GetComponent<ThinkFast.Player.PlayerAttack>());
            RebalanceAirAttack(enemy.GetComponent<ThinkFast.Enemy.EnemyAttack>());

            EditorSceneManager.MarkSceneDirty(fight);
            EditorSceneManager.SaveScene(fight);

            Debug.Log($"Imported the animated fighters from the art scene. Air swing rebalanced to {AirStartup}/{AirActive}/{AirRecovery}, hitbox re-centred at y {AirHitboxOffsetY} and move control back to {AirMoveControlScale}, so dives connect instead of passing under the target. Re-running 'Build PlayerController Test Scene' will replace them with the placeholder capsules again -- re-run this afterwards if you do.");
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
        /// Redistributes the air swing's phases on one fighter. Ground attacks are
        /// left exactly as animated: a grounded fighter is rooted through its own
        /// startup, so a late hitbox still lands where it was aimed, and the longer
        /// wind-up is the telegraph the player reads.
        /// </summary>
        private static void RebalanceAirAttack(Component attack)
        {
            if (attack == null)
            {
                return;
            }

            var so = new SerializedObject(attack);
            SerializedProperty air = so.FindProperty("airAttack");
            if (air == null)
            {
                Debug.LogWarning($"{attack.GetType().Name} has no airAttack to rebalance.", attack);
                return;
            }

            air.FindPropertyRelative("startup").floatValue = AirStartup;
            air.FindPropertyRelative("active").floatValue = AirActive;
            air.FindPropertyRelative("recovery").floatValue = AirRecovery;
            air.FindPropertyRelative("moveControlScale").floatValue = AirMoveControlScale;

            // Only the vertical part is restored. The horizontal reach is a real
            // reach decision and belongs to whoever animated the swing.
            SerializedProperty offset = air.FindPropertyRelative("hitboxOffset");
            Vector2 value = offset.vector2Value;
            value.y = AirHitboxOffsetY;
            offset.vector2Value = value;

            so.ApplyModifiedPropertiesWithoutUndo();
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
