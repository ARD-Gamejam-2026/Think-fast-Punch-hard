using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThinkFast.PlayerEditor
{
    /// <summary>
    /// The pieces a fighter stage is built out of: solid blocks, one-way
    /// platforms, and the materials both of them need.
    ///
    /// Lifted out of the test-rig builder when the tutorial gained a stage of
    /// its own. The platform is the reason this is shared rather than copied:
    /// its collider is authored at an exact width and the art is stretched to
    /// fit, never the other way round, and the art model carries a solid
    /// two-way collider that has to be switched off or the platform cannot be
    /// jumped up through. A second hand-written copy of those two rules would
    /// drift, and the first symptom would be a stage the opponent can no longer
    /// climb.
    /// </summary>
    public static class FighterStageParts
    {
        private const string PhysicsMaterialPath = "Assets/Settings/Physics/FighterNoFriction.physicsMaterial2D";
        private const string PlatformMaterialPath = "Assets/Settings/Materials/TestPlatform.mat";
        private const string PlatformPrefabPath = "Assets/Prefabs/Platform.prefab";

        /// <summary>Width of one platform art tile, as modelled.</summary>
        private const float PlatformArtWidth = 2f;

        /// <summary>Thickness of the platform art, which is thinner than the collider it sits in.</summary>
        private const float PlatformArtThickness = 0.25f;

        /// <summary>
        /// A 3D cube for looks with a BoxCollider2D for physics. The primitive's
        /// own 3D collider is stripped: gameplay collision is 2D only.
        /// </summary>
        public static GameObject CreateBlock(
            Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial2D frictionless)
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

        /// <summary>
        /// A floating platform you can jump up through and land on top of.
        /// One-way is the genre norm, and it keeps a missed jump from bonking you
        /// on the underside, which would make the arc much harder to read.
        /// </summary>
        public static GameObject CreatePlatform(
            Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial2D frictionless)
        {
            GameObject artPlatform = CreateArtPlatform(parent, name, position, scale, frictionless);
            if (artPlatform != null)
            {
                return artPlatform;
            }

            GameObject platform = CreateBlock(parent, name, position, scale, frictionless);

            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;

            // Slightly under a full 180 so clipping a corner on the way past does
            // not count as landing on it.
            effector.surfaceArc = 170f;

            platform.GetComponent<BoxCollider2D>().usedByEffector = true;

            Material material = GetOrCreatePlatformMaterial();
            if (material != null)
            {
                platform.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return platform;
        }

        /// <summary>
        /// Zero friction everywhere. Without it the capsule grabs walls on
        /// contact, which reads as a bug the moment you run into one.
        /// </summary>
        public static PhysicsMaterial2D GetOrCreateFrictionlessMaterial()
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

        /// <summary>
        /// Cosmetic only: if the URP shader cannot be found we return null and
        /// the caller falls back to the default material.
        /// </summary>
        public static Material GetOrCreateColourMaterial(string path, string name, Color colour)
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

        public static void EnsureAssetDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Blue, so one-way platforms are distinguishable from solid ground at a glance.</summary>
        private static Material GetOrCreatePlatformMaterial()
        {
            return GetOrCreateColourMaterial(PlatformMaterialPath, "TestPlatform", new Color(0.30f, 0.62f, 0.95f));
        }

        /// <summary>
        /// Builds a platform out of the art model, tiled across the span, with the
        /// collider still authored here.
        ///
        /// **The collider is deliberately not the model's.** The stage's spans are
        /// what the opponent's climbing routes are computed against, and the
        /// margins are thin -- the High Mid hop clears its gap by about half a
        /// unit. Laying whole 2-unit tiles end to end would round every platform's
        /// width to the nearest 2 and move its edges by up to a quarter of a unit,
        /// which is enough to put that hop out of reach. So the width stays
        /// exactly as authored and the art is stretched to fit it, never the other
        /// way round.
        ///
        /// The model also carries a solid two-way collider of its own, a metre
        /// tall, which would make the platform impossible to jump up through.
        /// Every collider on the art is switched off for that reason.
        ///
        /// Returns null when the art has not been imported, so the generated
        /// blocks remain the fallback.
        /// </summary>
        private static GameObject CreateArtPlatform(
            Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial2D frictionless)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPrefabPath);
            if (prefab == null)
            {
                return null;
            }

            var platform = new GameObject(name);
            platform.transform.SetParent(parent, worldPositionStays: false);
            platform.transform.localPosition = position;

            var box = platform.AddComponent<BoxCollider2D>();
            box.size = new Vector2(scale.x, scale.y);
            box.sharedMaterial = frictionless;
            box.usedByEffector = true;

            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;

            TileArt(platform.transform, prefab, scale);
            return platform;
        }

        /// <summary>
        /// Lays the art across the platform's width. Tile count is chosen to keep
        /// each one closest to its natural size, then they are stretched by the
        /// remainder -- at these widths that is under an eighth, which does not
        /// read on a slab.
        /// </summary>
        private static void TileArt(Transform platform, GameObject prefab, Vector3 scale)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(scale.x / PlatformArtWidth));
            float tileWidth = scale.x / count;
            float left = -scale.x * 0.5f;

            // The art is thinner than the collider. Aligning their tops rather
            // than stretching to match means the fighter stands on the surface it
            // can see, and the extra collider hangs below where nothing looks.
            float top = (scale.y * 0.5f) - (PlatformArtThickness * 0.5f);

            for (int i = 0; i < count; i++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, platform);
                tile.name = $"Art {i}";
                tile.transform.localPosition = new Vector3(left + (tileWidth * (i + 0.5f)), top, 0f);
                tile.transform.localScale = new Vector3(tileWidth / PlatformArtWidth, 1f, 1f);

                foreach (Collider2D collider in tile.GetComponentsInChildren<Collider2D>(includeInactive: true))
                {
                    collider.enabled = false;
                }
            }
        }
    }
}
