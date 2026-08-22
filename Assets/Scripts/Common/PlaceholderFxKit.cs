using UnityEngine;

namespace ThinkFast.Common
{
    /// <summary>
    /// THROWAWAY. The shared machinery behind the placeholder attack cues:
    /// synthesised one-shots, flat unlit materials, and primitives that animate
    /// themselves away.
    ///
    /// Everything is generated at runtime, so this drags in no assets. It exists
    /// only so the player's cues and the opponent's cues do not carry two copies
    /// of the same sample loop -- the two FX components that use it choose entirely
    /// different palettes, shapes and pitches, which is the part that matters.
    ///
    /// Deleting this and both FX components changes no gameplay.
    /// </summary>
    public static class PlaceholderFxKit
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Synthesises a one-shot: a sine sweeping from <paramref name="startHz"/>
        /// to <paramref name="endHz"/>, blended with noise and shaped by an
        /// exponential decay. Low and tonal reads as a thud; high and noisy reads
        /// as a whoosh; sweeping UP instead of down reads as a wind-up.
        /// </summary>
        public static AudioClip CreateClip(string name, float duration, float startHz, float endHz, float decay, float noiseMix, int seed)
        {
            const int SampleRate = 44100;

            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var samples = new float[sampleCount];
            var random = new System.Random(seed);

            // Phase is accumulated rather than computed from t, otherwise sweeping
            // the frequency would introduce audible discontinuities.
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / sampleCount;

                float hz = Mathf.Lerp(startHz, endHz, progress);
                phase += 2f * Mathf.PI * hz / SampleRate;

                float tone = Mathf.Sin(phase);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float envelope = Mathf.Exp(-t * decay);

                samples[i] = Mathf.Clamp(Mathf.Lerp(tone, noise, noiseMix) * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Unlit, so the cue stays a flat, bright, obviously-placeholder blob that
        /// no one will mistake for real art. Returns null if URP is missing, and
        /// callers fall back to the default material.
        /// </summary>
        public static Material CreateUnlitMaterial(Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            material.SetColor(BaseColorId, colour);
            return material;
        }

        /// <summary>
        /// Drops a primitive in the world that scales from <paramref name="from"/>
        /// to <paramref name="to"/> over its lifetime and then removes itself.
        ///
        /// Shrinking away reads as an impact that already happened; growing reads
        /// as something building up. That difference is the whole telegraph.
        /// </summary>
        public static GameObject SpawnShape(
            PrimitiveType type,
            Material material,
            Vector3 position,
            Vector3 from,
            Vector3 to,
            float lifetime)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = "PlaceholderFx";

            // Primitives come with a 3D collider. Gameplay is 2D, so it would
            // never collide with anything -- but it would still be queried.
            Collider collider = shape.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            if (material != null)
            {
                shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            // Deliberately not parented to the fighter: the cue marks where the
            // hitbox is, and should not follow anyone as they walk away.
            shape.transform.position = position;
            shape.transform.localScale = from;

            shape.AddComponent<PlaceholderFxShape>().Play(from, to, lifetime);
            return shape;
        }

        public static void DestroyAsset(Object asset)
        {
            if (asset != null)
            {
                Object.Destroy(asset);
            }
        }
    }
}
