using System.IO;
using ThinkFast.CameraRig;
using ThinkFast.Combat;
using ThinkFast.Economy;
using ThinkFast.Player;
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
        private const string PhysicsMaterialPath = "Assets/Settings/Physics/FighterNoFriction.physicsMaterial2D";
        private const string PlatformMaterialPath = "Assets/Settings/Materials/TestPlatform.mat";
        private const string DummyMaterialPath = "Assets/Settings/Materials/TestDummy.mat";

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

            PhysicsMaterial2D frictionless = GetOrCreateFrictionlessMaterial();

            BuildStage(root.transform, frictionless);
            GameObject player = BuildPlayer(root.transform, frictionless);
            BuildTrainingDummy(root.transform, frictionless, new Vector3(2f, 1.2f, 0f));
            FrameCamera(scene, player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = player;
            Debug.Log($"Built PlayerController test rig in {ScenePath}. Press Play, then move with A/D or the arrow keys.");
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
            CreateBlock(parent, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 4f), frictionless);
            CreateBlock(parent, "Wall Left", new Vector3(-15f, 2.5f, 0f), new Vector3(1f, 6f, 4f), frictionless);
            CreateBlock(parent, "Wall Right", new Vector3(15f, 2.5f, 0f), new Vector3(1f, 6f, 4f), frictionless);

            // A short step, to make it obvious when the ground check works and
            // when it does not.
            CreateBlock(parent, "Step", new Vector3(6f, 0.25f, 0f), new Vector3(4f, 0.5f, 4f), frictionless);

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
            Material material = GetOrCreatePlatformMaterial();
            var thickness = 0.4f;

            // Position is the centre, so a platform's walkable surface sits at
            // y + thickness/2.
            CreatePlatform(parent, "Platform Low Left", new Vector3(-9f, 2.4f, 0f), new Vector3(4.5f, thickness, 4f), frictionless, material);
            CreatePlatform(parent, "Platform High Mid", new Vector3(-2f, 4.4f, 0f), new Vector3(4f, thickness, 4f), frictionless, material);
            CreatePlatform(parent, "Platform Low Right", new Vector3(5f, 2.4f, 0f), new Vector3(4f, thickness, 4f), frictionless, material);
            CreatePlatform(parent, "Platform Top Right", new Vector3(11f, 4.8f, 0f), new Vector3(3.5f, thickness, 4f), frictionless, material);
        }

        /// <summary>
        /// A floating platform you can jump up through and land on top of.
        /// One-way is the genre norm, and it keeps a missed jump from bonking you
        /// on the underside, which would make the arc much harder to read.
        /// </summary>
        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial2D frictionless, Material material)
        {
            GameObject platform = CreateBlock(parent, name, position, scale, frictionless);

            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;

            // Slightly under a full 180 so clipping a corner on the way past does
            // not count as landing on it.
            effector.surfaceArc = 170f;

            platform.GetComponent<BoxCollider2D>().usedByEffector = true;

            if (material != null)
            {
                platform.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return platform;
        }

        /// <summary>
        /// A 3D cube for looks with a BoxCollider2D for physics. The primitive's
        /// own 3D collider is stripped: gameplay collision is 2D only.
        /// </summary>
        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial2D frictionless)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, worldPositionStays: false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;

            Object.DestroyImmediate(block.GetComponent<Collider>());

            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = frictionless;

            return block;
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
            AssignVisualRoot(controller, visual.transform);

            // Resources must exist before PlayerAttack, which looks them up in
            // Awake to decide whether attacks cost anything.
            player.AddComponent<FighterResources>();

            player.AddComponent<Health>();
            player.AddComponent<PlayerHitReaction>();

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
        /// A punching bag standing in for the AI opponent. Given the same physics
        /// setup as the fighter so knockback reads the same way it will later,
        /// but with nothing driving it.
        /// </summary>
        private static GameObject BuildTrainingDummy(Transform parent, PhysicsMaterial2D frictionless, Vector3 position)
        {
            var dummy = new GameObject("Training Dummy");
            dummy.transform.SetParent(parent, worldPositionStays: false);
            dummy.transform.localPosition = position;

            var body = dummy.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Drag, so a launched dummy comes to rest instead of sliding to the
            // far wall on every hit. The stage is frictionless, so this damping
            // is the ONLY thing stopping it. Roughly: it travels knockback/damping
            // units, so ~11/2.5 is a bit over four units per punch.
            body.linearDamping = 2.5f;

            var capsule = dummy.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.8f, 1.8f);
            capsule.sharedMaterial = frictionless;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(dummy.transform, worldPositionStays: false);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            Material material = GetOrCreateDummyMaterial();
            if (material != null)
            {
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            dummy.AddComponent<TrainingDummy>();
            return dummy;
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

        private static void AssignVisualRoot(PlayerController controller, Transform visual)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Zero friction everywhere. Without it the capsule grabs walls on
        /// contact, which reads as a bug the moment you run into one.
        /// </summary>
        private static PhysicsMaterial2D GetOrCreateFrictionlessMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureAssetDirectory(PhysicsMaterialPath);

            var material = new PhysicsMaterial2D("FighterNoFriction")
            {
                friction = 0f,
                bounciness = 0f,
            };

            AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        /// <summary>Blue, so one-way platforms are distinguishable from solid ground at a glance.</summary>
        private static Material GetOrCreatePlatformMaterial()
        {
            return GetOrCreateColourMaterial(PlatformMaterialPath, "TestPlatform", new Color(0.30f, 0.62f, 0.95f));
        }

        /// <summary>
        /// Pale grey, so the dummy's red hit-flash is unmistakable against it.
        /// The flash is driven by a property block at runtime, which needs a
        /// material with a _BaseColor to override.
        /// </summary>
        private static Material GetOrCreateDummyMaterial()
        {
            return GetOrCreateColourMaterial(DummyMaterialPath, "TestDummy", new Color(0.78f, 0.78f, 0.80f));
        }

        /// <summary>
        /// Cosmetic only: if the URP shader cannot be found we return null and
        /// the caller falls back to the default material.
        /// </summary>
        private static Material GetOrCreateColourMaterial(string path, string name, Color colour)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning($"URP Lit shader not found; '{name}' will use the default material.");
                return null;
            }

            EnsureAssetDirectory(path);

            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", colour);

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
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
