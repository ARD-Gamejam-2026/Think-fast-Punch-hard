using ThinkFast.CameraRig;
using ThinkFast.Combat;
using ThinkFast.Economy;
using ThinkFast.Enemy;
using ThinkFast.Player;
using ThinkFast.Rounds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ThinkFast.PlayerEditor
{
    /// <summary>
    /// Builds (and rebuilds) the movement test rig inside
    /// Assets/Scenes/PlayerControllerTest.unity.
    ///
    /// The rig is generated rather than hand-placed so it stays reproducible: it
    /// is safe to re-run at any time, and it always tears down what it made last
    /// time before making it again.
    /// </summary>
    public static class PlayerControllerTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PlayerControllerTest.unity";
        private const string InputAssetPath = "Assets/Settings/Input/FighterControls.inputactions";
        private const string EnemyMaterialPath = "Assets/Settings/Materials/TestEnemy.mat";

        /// <summary>Root object name. Everything generated lives under it, so cleanup is one delete.</summary>
        private const string RigRootName = "--- Test Rig (generated) ---";


        [MenuItem("Tools/Think Fast/Build PlayerController Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemoveExistingRig(scene);

            var root = new GameObject(RigRootName);
            Undo.RegisterCreatedObjectUndo(root, "Build PlayerController Test Scene");

            PhysicsMaterial2D frictionless = FighterStageParts.GetOrCreateFrictionlessMaterial();

            BuildStage(root.transform, frictionless);
            // On the floor and clear of the step, far enough away that the fight
            // opens with the opponent walking at you rather than already inside
            // your guard on frame one.
            var playerSpawn = new Vector3(-4f, 1.2f, 0f);
            var enemySpawn = new Vector3(3f, 1.2f, 0f);

            // Prefabs first, so a rebuild of the stage keeps the animated fighters
            // and their tuning. Generating them is the fallback for a project that
            // has not saved them yet -- see FighterPrefabs.
            GameObject player = Spawn(FighterPrefabs.PlayerPath, root.transform, playerSpawn)
                ?? BuildPlayer(root.transform, frictionless);

            GameObject enemy = Spawn(FighterPrefabs.EnemyPath, root.transform, enemySpawn)
                ?? BuildEnemy(root.transform, frictionless, enemySpawn, player.transform);

            // A spawned prefab has no idea what it is fighting: the target is a
            // scene object, so it cannot be stored in the asset.
            var brain = enemy.GetComponent<ThinkFast.Enemy.EnemyBrain>();
            if (brain != null)
            {
                AssignObjectField(brain, "target", player.transform);
            }
            BuildRoundDebug(root.transform);
            FrameCamera(scene, player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = player;
            Debug.Log($"Built PlayerController test rig in {ScenePath}. Press Play, then move with A/D or the arrow keys. The opponent fights back; R resets it, Enter restarts after a K.O.");
        }

        private static void RemoveExistingRig(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == RigRootName)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void BuildStage(Transform parent, PhysicsMaterial2D frictionless)
        {
            // Floor, plus a wall at each end so horizontal movement has something
            // to run into.
            FighterStageParts.CreateBlock(parent, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 4f), frictionless);
            FighterStageParts.CreateBlock(parent, "Wall Left", new Vector3(-15f, 2.5f, 0f), new Vector3(1f, 6f, 4f), frictionless);
            FighterStageParts.CreateBlock(parent, "Wall Right", new Vector3(15f, 2.5f, 0f), new Vector3(1f, 6f, 4f), frictionless);

            // A short step, to make it obvious when the ground check works and
            // when it does not.
            FighterStageParts.CreateBlock(parent, "Step", new Vector3(6f, 0.25f, 0f), new Vector3(4f, 0.5f, 4f), frictionless);

            BuildPlatforms(parent, frictionless);
        }

        /// <summary>
        /// A climb of floating platforms. Heights are picked against the default
        /// 3.2-unit jump apex: the low platforms are reachable from the floor,
        /// the high ones only by chaining jumps off the low ones. Gaps are inside
        /// a running jump's reach, but not by much.
        /// </summary>
        private static void BuildPlatforms(Transform parent, PhysicsMaterial2D frictionless)
        {
            var thickness = 0.4f;

            // Position is the centre, so a platform's walkable surface sits at
            // y + thickness/2.
            FighterStageParts.CreatePlatform(parent, "Platform Low Left", new Vector3(-9f, 2.4f, 0f), new Vector3(4.5f, thickness, 4f), frictionless);
            FighterStageParts.CreatePlatform(parent, "Platform High Mid", new Vector3(-2f, 4.4f, 0f), new Vector3(4f, thickness, 4f), frictionless);
            FighterStageParts.CreatePlatform(parent, "Platform Low Right", new Vector3(5f, 2.4f, 0f), new Vector3(4f, thickness, 4f), frictionless);
            FighterStageParts.CreatePlatform(parent, "Platform Top Right", new Vector3(11f, 4.8f, 0f), new Vector3(3.5f, thickness, 4f), frictionless);
        }

        /// <summary>
        /// Instantiates a saved fighter prefab into the rig, or returns null when
        /// none has been saved so the caller can generate one instead.
        /// </summary>
        private static GameObject Spawn(string prefabPath, Transform parent, Vector3 position)
        {
            GameObject prefab = FighterPrefabs.Load(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position;
            return instance;
        }

        private static GameObject BuildPlayer(Transform parent, PhysicsMaterial2D frictionless)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(parent, worldPositionStays: false);
            player.transform.localPosition = new Vector3(-4f, 1.2f, 0f);

            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.8f, 1.8f);
            capsule.sharedMaterial = frictionless;

            // 3D art, 2D physics: the mesh is presentation only.
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform, worldPositionStays: false);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // A nose, so which way the fighter faces is readable at a glance.
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing Marker";
            nose.transform.SetParent(visual.transform, worldPositionStays: false);
            nose.transform.localPosition = new Vector3(0.6f, 0.35f, 0f);
            nose.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());

            var input = player.AddComponent<PlayerInputReader>();
            AssignInputAsset(input);

            var controller = player.AddComponent<PlayerController>();
            AssignObjectField(controller, "visualRoot", visual.transform);

            // Resources must exist before PlayerAttack, which looks them up in
            // Awake to decide whether attacks cost anything.
            player.AddComponent<FighterResources>();

            player.AddComponent<Health>();
            player.AddComponent<FighterHitReaction>();
            player.AddComponent<PlayerKnockout>();

            // Throwaway: press H to take a canned hit, so damage and hitstun can
            // be felt before any opponent exists that could deal them.
            player.AddComponent<DebugSelfDamage>();

            player.AddComponent<PlayerAttack>();
            player.AddComponent<PlayerDebugHud>();

            // Stand-in for the puzzle half, and the placeholder Flow state cue.
            // Both are throwaway; delete them once the real systems land.
            player.AddComponent<DebugRiddleDriver>();
            player.AddComponent<PlaceholderFlowStateVisual>();

            // Placeholder audio/visual cues. Safe to delete the component (and
            // its script) once real art and audio land.
            player.AddComponent<PlaceholderAttackFx>();

            return player;
        }

        /// <summary>
        /// The AI opponent. Given the same body as the player -- same collider,
        /// same frictionless material, no linear damping -- so knockback reads
        /// identically on both fighters and a hit can be tuned once.
        /// </summary>
        private static GameObject BuildEnemy(Transform parent, PhysicsMaterial2D frictionless, Vector3 position, Transform target)
        {
            var enemy = new GameObject("Enemy");
            enemy.transform.SetParent(parent, worldPositionStays: false);
            enemy.transform.localPosition = position;

            var body = enemy.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // No linear damping, unlike the dummy this replaces. The dummy needed
            // it because nothing else ever stopped it sliding; the opponent
            // brakes itself through EnemyMotor, and damping would quietly fight
            // its own acceleration curve.
            body.linearDamping = 0f;

            var capsule = enemy.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.8f, 1.8f);
            capsule.sharedMaterial = frictionless;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(enemy.transform, worldPositionStays: false);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // A nose, so which way it is facing -- and therefore where its next
            // hitbox lands -- is readable at a glance.
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing Marker";
            nose.transform.SetParent(visual.transform, worldPositionStays: false);
            nose.transform.localPosition = new Vector3(0.6f, 0.35f, 0f);
            nose.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());

            Material material = GetOrCreateEnemyMaterial();
            if (material != null)
            {
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;
                nose.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            var motor = enemy.AddComponent<EnemyMotor>();
            AssignObjectField(motor, "visualRoot", visual.transform);

            enemy.AddComponent<Health>();

            // EnemyKnockout decides the round is over; FighterHitReaction --
            // the same component the player uses -- decides how the body reacts
            // while it happens. The flash is enabled here and not on the player,
            // whose Flow-state visual already owns those renderers.
            enemy.AddComponent<EnemyKnockout>();
            var hitReaction = enemy.AddComponent<FighterHitReaction>();
            AssignBoolField(hitReaction, "flashOnHit", true);

            enemy.AddComponent<EnemyAttack>();

            var brain = enemy.AddComponent<EnemyBrain>();
            AssignObjectField(brain, "target", target);

            enemy.AddComponent<EnemyDebugHud>();

            // Warns on Play if the tuning numbers have drifted out of agreement
            // with each other. Costs nothing in a release build.
            enemy.AddComponent<EnemyTuningCheck>();

            // Placeholder cues, and the wind-up telegraph in particular: without
            // it the opponent's startup looks like a stall rather than a swing.
            // Throwaway, like the player's -- delete the component and its script
            // once real art and audio land.
            enemy.AddComponent<PlaceholderEnemyAttackFx>();

            return enemy;
        }

        /// <summary>
        /// Throwaway round-result banner. Its own object rather than a component
        /// on a fighter, because the round outcome is not any one fighter's
        /// business -- and because deleting it later should be one delete.
        /// </summary>
        private static GameObject BuildRoundDebug(Transform parent)
        {
            var round = new GameObject("Round Debug");
            round.transform.SetParent(parent, worldPositionStays: false);
            round.AddComponent<DebugRoundBanner>();
            return round;
        }

        private static void AssignInputAsset(PlayerInputReader reader)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (asset == null)
            {
                Debug.LogError($"Could not load the fighter input asset at {InputAssetPath}.");
                return;
            }

            var so = new SerializedObject(reader);
            so.FindProperty("actions").objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Writes a private [SerializeField] object reference. Goes through
        /// SerializedObject because the fields are private by design -- the
        /// alternative is loosening them to public purely so a build script can
        /// reach them.
        /// </summary>
        /// <summary>Writes a private [SerializeField] bool. Same reasoning as AssignObjectField.</summary>
        private static void AssignBoolField(Object component, string fieldName, bool value)
        {
            var so = new SerializedObject(component);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"'{component.GetType().Name}' has no serialized field '{fieldName}'.");
                return;
            }

            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObjectField(Object component, string fieldName, Object value)
        {
            var so = new SerializedObject(component);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"'{component.GetType().Name}' has no serialized field '{fieldName}'.");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Muted purple: dark enough to read as "the other fighter" against the
        /// grey stage, pale enough that the red hit-flash and the grey knockout
        /// tint are both unmistakable. Both are driven by a property block at
        /// runtime, which needs a material with a _BaseColor to override.
        /// </summary>
        private static Material GetOrCreateEnemyMaterial()
        {
            return FighterStageParts.GetOrCreateColourMaterial(EnemyMaterialPath, "TestEnemy", new Color(0.62f, 0.48f, 0.72f));
        }

        private static void FrameCamera(Scene scene, Transform player)
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            // Pulled in from -16 to -9. At -16 a 60 degree FOV covers roughly 33
            // units, which is wider than the whole 30-unit stage -- so the camera
            // had nothing it could ever follow. Following only means anything
            // once the view is narrower than the level.
            camera.transform.position = new Vector3(0f, 3f, -9f);
            camera.transform.rotation = Quaternion.identity;

            // The camera lives outside the generated rig, so a rebuild does not
            // remove it. Reuse the component if it is already there and just
            // re-point it at the new player.
            var follow = camera.GetComponent<FollowCamera>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<FollowCamera>();
            }

            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
