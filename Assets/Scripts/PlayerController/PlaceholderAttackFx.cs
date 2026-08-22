using ThinkFast.Combat;
using ThinkFast.Common;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// THROWAWAY. Placeholder sound and visuals so the two attacks are tellable
    /// apart before any real art or audio exists.
    ///
    /// Everything here is generated at runtime -- see
    /// <see cref="PlaceholderFxKit"/> -- so this drags in no assets at all. It
    /// listens to <see cref="PlayerAttack"/> events and is never called into, so
    /// deleting this one file and its component removes it completely, with no
    /// changes needed anywhere else.
    ///
    /// Palette and shapes are chosen to contrast with the opponent's cues in
    /// <see cref="ThinkFast.Enemy.PlaceholderEnemyAttackFx"/>: warm colours and
    /// hard-edged shapes here, violet and rounded there.
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
            groundSwingClip = PlaceholderFxKit.CreateClip("PlaceholderGroundSwing", 0.20f, 190f, 70f, 16f, 0.15f, seed: 11);
            airSwingClip = PlaceholderFxKit.CreateClip("PlaceholderAirSwing", 0.14f, 900f, 380f, 20f, 0.75f, seed: 22);
            impactClip = PlaceholderFxKit.CreateClip("PlaceholderImpact", 0.16f, 150f, 55f, 24f, 0.40f, seed: 33);

            // Deliberately weak and dull: "nothing happened" has to sound
            // different from a punch, or being out of AP reads as a broken button.
            refusedClip = PlaceholderFxKit.CreateClip("PlaceholderRefused", 0.09f, 240f, 210f, 34f, 0.05f, seed: 44);

            groundMaterial = PlaceholderFxKit.CreateUnlitMaterial(groundColour);
            airMaterial = PlaceholderFxKit.CreateUnlitMaterial(airColour);
            impactMaterial = PlaceholderFxKit.CreateUnlitMaterial(impactColour);
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
            PlaceholderFxKit.DestroyAsset(groundSwingClip);
            PlaceholderFxKit.DestroyAsset(airSwingClip);
            PlaceholderFxKit.DestroyAsset(impactClip);
            PlaceholderFxKit.DestroyAsset(refusedClip);
            PlaceholderFxKit.DestroyAsset(groundMaterial);
            PlaceholderFxKit.DestroyAsset(airMaterial);
            PlaceholderFxKit.DestroyAsset(impactMaterial);
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
            PlaceholderFxKit.SpawnShape(
                type,
                material,
                new Vector3(position.x, position.y, transform.position.z),
                scale,
                Vector3.zero,
                shapeLifetime);
        }
    }
}
