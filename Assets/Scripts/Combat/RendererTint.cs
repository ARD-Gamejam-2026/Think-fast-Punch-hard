using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// Flashes a set of renderers a flat colour and puts them back again.
    ///
    /// Goes through a <see cref="MaterialPropertyBlock"/> rather than touching
    /// the material, which matters for two reasons: assigning to
    /// <c>Renderer.material</c> silently instantiates a copy that then leaks, and
    /// editing the shared material would tint every other object using it.
    ///
    /// Placeholder-grade on purpose -- a real hit flash belongs in the shader
    /// once there is art. Until then this is the only feedback that a hit
    /// connected, so it earns its place.
    /// </summary>
    public sealed class RendererTint
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Renderer[] renderers;
        private readonly Color[] baseColours;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        public RendererTint(Renderer[] renderers)
        {
            this.renderers = renderers ?? new Renderer[0];
            baseColours = new Color[this.renderers.Length];

            for (int i = 0; i < this.renderers.Length; i++)
            {
                Material material = this.renderers[i] != null ? this.renderers[i].sharedMaterial : null;
                baseColours[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
        }

        /// <summary>Tints everything the given colour.</summary>
        public void Set(Color colour)
        {
            Apply(i => colour);
        }

        /// <summary>Restores the colour each renderer started with.</summary>
        public void Clear()
        {
            Apply(i => baseColours[i]);
        }

        private void Apply(System.Func<int, Color> colourFor)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, colourFor(i));
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
