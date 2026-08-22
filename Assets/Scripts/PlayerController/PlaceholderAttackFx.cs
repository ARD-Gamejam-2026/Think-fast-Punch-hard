using System.Collections;
using ThinkFast.Combat;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// THROWAWAY. Placeholder sound and visuals so the two attacks are tellable
    /// apart before any real art or audio exists.
    ///
    /// Everything here is generated at runtime -- the sounds are synthesised
    /// sample by sample and the visuals are Unity primitives -- so this drags in
    /// no assets at all. It listens to <see cref="PlayerAttack"/> events and is
    /// never called into, so deleting this one file and its GameObject removes it
    /// completely, with no changes needed anywhere else.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAttack))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlaceholderAttackFx : MonoBehaviour
    {
        [Header("Ground attack")]
        [SerializeField] private Color groundColour = new Color(1f, 0.45f, 0.1f);

        [Header("Air attack")]
        [SerializeField] private Color airColour = new Color(0.3f, 0.9f, 1f);

        [Header("Impact")]
        [SerializeField] private Color impactColour = new Color(1f, 0.95f, 0.4f);

        [Header("Tuning")]
        [Tooltip("How long a spawned shape takes to shrink away.")]
        [SerializeField] private float shapeLifetime = 0.16f;

        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

        private PlayerAttack attack;
        private AudioSource audioSource;

        private AudioClip groundSwingClip;
        private AudioClip airSwingClip;
        private AudioClip impactClip;
        private AudioClip refusedClip;

        private Material groundMaterial;
        private Material airMaterial;
        private Material impactMaterial;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            attack = GetComponent<PlayerAttack>();

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // 2D: this is a UI-ish placeholder cue, not a positioned game sound.
            audioSource.spatialBlend = 0f;

            // A low, slow thud versus a high, airy hiss. Pitch and noise content
            // are what actually make the two readable apart by ear -- volume and
            // length alone are not enough.
            groundSwingClip = CreateClip("PlaceholderGroundSwing", 0.20f, 190f, 70f, 16f, 0.15f, seed: 11);
            airSwingClip = CreateClip("PlaceholderAirSwing", 0.14f, 900f, 380f, 20f, 0.75f, seed: 22);
            impactClip = CreateClip("PlaceholderImpact", 0.16f, 150f, 55f, 24f, 0.40f, seed: 33);

            // Deliberately weak and dull: "nothing happened" has to sound
            // different from a punch, or being out of AP reads as a broken button.
            refusedClip = CreateClip("PlaceholderRefused", 0.09f, 240f, 210f, 34f, 0.05f, seed: 44);

            groundMaterial = CreateUnlitMaterial(groundColour);
            airMaterial = CreateUnlitMaterial(airColour);
            impactMaterial = CreateUnlitMaterial(impactColour);
        }

        private void OnEnable()
        {
            attack.AttackBecameActive += HandleAttackBecameActive;
            attack.HitLanded += HandleHitLanded;
            attack.AttackRefused += HandleAttackRefused;
        }

        private void OnDisable()
        {
            attack.AttackBecameActive -= HandleAttackBecameActive;
            attack.HitLanded -= HandleHitLanded;
            attack.AttackRefused -= HandleAttackRefused;
        }

        private void OnDestroy()
        {
            // Runtime-created assets are not owned by the scene, so they have to
            // be cleaned up by hand.
            DestroyAsset(groundSwingClip);
            DestroyAsset(airSwingClip);
            DestroyAsset(impactClip);
            DestroyAsset(refusedClip);
            DestroyAsset(groundMaterial);
            DestroyAsset(airMaterial);
            DestroyAsset(impactMaterial);
        }

        private void HandleAttackBecameActive(AttackDefinition definition, Vector2 centre)
        {
            bool isGround = definition == attack.GroundAttack;

            audioSource.PlayOneShot(isGround ? groundSwingClip : airSwingClip, volume);

            // Different shape as well as different colour: shape survives being
            // glanced at in peripheral vision, colour alone does not.
            SpawnShape(
                isGround ? PrimitiveType.Cube : PrimitiveType.Sphere,
                isGround ? groundMaterial : airMaterial,
                centre,
                new Vector3(definition.hitboxSize.x, definition.hitboxSize.y, 0.3f));
        }

        private void HandleHitLanded(AttackDefinition definition, Vector2 point)
        {
            audioSource.PlayOneShot(impactClip, volume);
            SpawnShape(PrimitiveType.Sphere, impactMaterial, point, Vector3.one * 0.7f);
        }

        private void HandleAttackRefused()
        {
            // Sound only, and quiet. No shape: nothing came out, so drawing
            // something where the hitbox would have been would be a lie.
            audioSource.PlayOneShot(refusedClip, volume * 0.5f);
        }

        private void SpawnShape(PrimitiveType type, Material material, Vector2 position, Vector3 scale)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = "PlaceholderFx";

            // Primitives come with a 3D collider. Gameplay is 2D, so it would
            // never collide with anything -- but it would still be queried.
            Collider collider = shape.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (material != null)
            {
                shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            // Deliberately not parented to the fighter: the cue marks where the
            // hitbox was, and should not follow you as you walk away.
            shape.transform.position = new Vector3(position.x, position.y, transform.position.z);
            shape.transform.localScale = scale;

            // Destroy is the guarantee; the coroutine is only cosmetic, so a
            // stopped coroutine can never leak an object.
            Destroy(shape, shapeLifetime);
            StartCoroutine(ShrinkAway(shape.transform, scale, shapeLifetime));
        }

        private static IEnumerator ShrinkAway(Transform target, Vector3 from, float lifetime)
        {
            float elapsed = 0f;
            while (target != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(from, Vector3.zero, Mathf.Clamp01(elapsed / lifetime));
                yield return null;
            }
        }

        /// <summary>
        /// Synthesises a one-shot: a sine sweeping from <paramref name="startHz"/>
        /// down to <paramref name="endHz"/>, blended with noise and shaped by an
        /// exponential decay. Low and tonal reads as a thud; high and noisy reads
        /// as a whoosh.
        /// </summary>
        private static AudioClip CreateClip(string name, float duration, float startHz, float endHz, float decay, float noiseMix, int seed)
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

        private static Material CreateUnlitMaterial(Color colour)
        {
            // Unlit so the cue stays a flat, bright, obviously-placeholder blob
            // that no one will mistake for real art.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            material.SetColor(BaseColorId, colour);
            return material;
        }

        private static void DestroyAsset(Object asset)
        {
            if (asset != null)
            {
                Destroy(asset);
            }
        }
    }
}
