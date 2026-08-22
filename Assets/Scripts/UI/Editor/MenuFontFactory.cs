using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Builds the TextMeshPro font asset the menus use, from the Nunito TrueType
    /// file in the project.
    ///
    /// Nunito is a rounded sans, which is most of what makes this style read the
    /// way it does -- the shapes and colours get you clean, the letterforms get
    /// you friendly. It ships under the SIL Open Font License, and the licence
    /// text sits beside it in Assets/Fonts.
    ///
    /// The atlas is dynamic, so glyphs are rasterised as they are first used
    /// rather than baked up front. That keeps the asset small and means a label
    /// added later cannot come out as missing-glyph boxes.
    /// </summary>
    public static class MenuFontFactory
    {
        private const string SourcePath = "Assets/Fonts/Nunito.ttf";
        private const string AssetPath = "Assets/Fonts/Nunito SDF.asset";

        /// <summary>
        /// Returns the menu font asset, creating it from the TrueType file the
        /// first time. Returns null when the font is missing, and says so.
        /// </summary>
        public static TMP_FontAsset EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (existing != null)
            {
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (source == null)
            {
                Debug.LogError($"No font at {SourcePath}. The menus will fall back to the default TextMeshPro font.");
                return null;
            }

            // 90 px sampling with 9 px padding: enough that the signed distance
            // field still has room to describe a rounded corner at the sizes the
            // title is set in, without a needlessly large atlas.
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (asset == null)
            {
                Debug.LogError($"Could not build a font asset from {SourcePath}.");
                return null;
            }

            asset.name = "Nunito SDF";
            AssetDatabase.CreateAsset(asset, AssetPath);

            // The atlas texture and material are created in memory alongside the
            // asset. Without folding them in they are lost on reload, and the font
            // comes back as an unreadable pink block.
            AddSubAsset(asset.material, asset, "Nunito SDF Material");

            if (asset.atlasTextures != null && asset.atlasTextures.Length > 0)
            {
                AddSubAsset(asset.atlasTextures[0], asset, "Nunito Atlas");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created {AssetPath} from {SourcePath}.");
            return asset;
        }

        private static void AddSubAsset(Object child, Object parent, string name)
        {
            if (child == null)
            {
                return;
            }

            child.name = name;
            AssetDatabase.AddObjectToAsset(child, parent);
        }
    }
}
