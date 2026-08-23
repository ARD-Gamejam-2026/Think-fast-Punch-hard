using ThinkFast.Combat;
using ThinkFast.Common;
using UnityEngine;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// THROWAWAY. The opponent's attack cues, deliberately unlike the player's.
    ///
    /// Two jobs, and the first is the important one:
    ///
    /// 1. **Telegraph the wind-up.** Startup is the longest phase of the swing,
    ///    and with nothing drawn during it the opponent appears to stop dead and
    ///    then hit you -- which reads as the game hitching, not as an attack
    ///    being charged. A shape that GROWS through startup turns dead time into
    ///    information: you can see the punch coming and where it will land.
    /// 2. **Sound and look like someone else.** Violet against the player's
    ///    orange and cyan, cylinders and capsules against cubes and spheres, and
    ///    pitched well below the player's swings. In a busy exchange it must be
    ///    obvious at a glance whose hitbox just appeared.
    ///
    /// Listens to <see cref="EnemyAttack"/> and is never called into, so deleting
    /// this one file and its component removes it completely.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlaceholderEnemyAttackFx : MonoBehaviour
    {
        [Header("Wind-up")]
        [Tooltip("The telegraph. Deliberately dim -- it is a warning, not the hit.")]
        [SerializeField] private Color windUpColour = new Color(0.72f, 0.25f, 0.95f, 1f);

        [Tooltip("Fraction of the hitbox the telegraph starts at. It grows to full size over the startup, so 'how big is it now' reads as 'how soon is it'.")]
        [SerializeField, Range(0.05f, 1f)] private float windUpStartScale = 0.2f;

        [Header("Swing")]
        [SerializeField] private Color groundColour = new Color(0.85f, 0.15f, 0.75f);

        [SerializeField] private Color airColour = new Color(0.55f, 0.35f, 1f);

        [Header("Impact")]
        [SerializeField] private Color impactColour = new Color(1f, 0.55f, 0.85f);

        [Header("Tuning")]
        [Tooltip("How long a spawned swing or impact shape takes to shrink away.")]
        [SerializeField] private float shapeLifetime = 0.16f;

        [Tooltip("Kept under the player's, so the opponent never drowns out your own hits.")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.4f;

        private EnemyAttack attack;
        private AudioSource audioSource;

        [SerializeField] private AudioClip windUpClip;
        [SerializeField] private AudioClip groundSwingClip;
        [SerializeField] private AudioClip airSwingClip;
        [SerializeField] private AudioClip impactClip;

        private Material windUpMaterial;
        private Material groundMaterial;
        private Material airMaterial;
        private Material impactMaterial;

        private void Awake()
        {
            attack = GetComponent<EnemyAttack>();

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            // The wind-up sweeps UP where every other cue sweeps down. A rising
            // pitch is heard as something about to happen; a falling one is heard
            // as something that already did.
            windUpClip = PlaceholderFxKit.CreateClip("EnemyWindUp", 0.20f, 90f, 260f, 3f, 0.05f, seed: 51);

            // Both swings sit below the player's 190Hz thud and 900Hz hiss, so
            // whose attack you are hearing needs no thought.
            groundSwingClip = PlaceholderFxKit.CreateClip("EnemyGroundSwing", 0.22f, 130f, 45f, 14f, 0.30f, seed: 52);
            airSwingClip = PlaceholderFxKit.CreateClip("EnemyAirSwing", 0.16f, 520f, 200f, 18f, 0.60f, seed: 53);
            impactClip = PlaceholderFxKit.CreateClip("EnemyImpact", 0.18f, 110f, 40f, 20f, 0.50f, seed: 54);

            windUpMaterial = PlaceholderFxKit.CreateUnlitMaterial(windUpColour);
            groundMaterial = PlaceholderFxKit.CreateUnlitMaterial(groundColour);
            airMaterial = PlaceholderFxKit.CreateUnlitMaterial(airColour);
            impactMaterial = PlaceholderFxKit.CreateUnlitMaterial(impactColour);
        }

        private void OnEnable()
        {
            attack.AttackStarted += HandleAttackStarted;
            attack.AttackBecameActive += HandleAttackBecameActive;
            attack.HitLanded += HandleHitLanded;
        }

        private void OnDisable()
        {
            attack.AttackStarted -= HandleAttackStarted;
            attack.AttackBecameActive -= HandleAttackBecameActive;
            attack.HitLanded -= HandleHitLanded;
        }

        private void OnDestroy()
        {
            // Runtime-created assets are not owned by the scene, so they have to
            // be cleaned up by hand.
            PlaceholderFxKit.DestroyAsset(windUpClip);
            PlaceholderFxKit.DestroyAsset(groundSwingClip);
            PlaceholderFxKit.DestroyAsset(airSwingClip);
            PlaceholderFxKit.DestroyAsset(impactClip);
            PlaceholderFxKit.DestroyAsset(windUpMaterial);
            PlaceholderFxKit.DestroyAsset(groundMaterial);
            PlaceholderFxKit.DestroyAsset(airMaterial);
            PlaceholderFxKit.DestroyAsset(impactMaterial);
        }

        private void HandleAttackStarted(AttackDefinition definition, Vector2 centre)
        {
            audioSource.PlayOneShot(windUpClip, volume * 0.7f);

            // Lives exactly as long as the startup and ends at the true hitbox
            // size, so the moment it stops growing is the moment it becomes
            // dangerous. Any other lifetime would be lying about the timing.
            var full = new Vector3(definition.hitboxSize.x, definition.hitboxSize.y, 0.25f);
            PlaceholderFxKit.SpawnShape(
                PrimitiveType.Cylinder,
                windUpMaterial,
                Position(centre),
                full * windUpStartScale,
                full,
                definition.startup);
        }

        private void HandleAttackBecameActive(AttackDefinition definition, Vector2 centre)
        {
            bool isGround = definition == attack.GroundAttack;

            audioSource.PlayOneShot(isGround ? groundSwingClip : airSwingClip, volume);

            // Capsules, where the player uses cubes and spheres. Shape survives
            // being glanced at in peripheral vision; colour alone does not.
            var size = new Vector3(definition.hitboxSize.x, definition.hitboxSize.y, 0.3f);
            PlaceholderFxKit.SpawnShape(
                PrimitiveType.Capsule,
                isGround ? groundMaterial : airMaterial,
                Position(centre),
                size,
                Vector3.zero,
                shapeLifetime);
        }

        private void HandleHitLanded(AttackDefinition definition, Vector2 point)
        {
            audioSource.PlayOneShot(impactClip, volume);
            PlaceholderFxKit.SpawnShape(
                PrimitiveType.Capsule,
                impactMaterial,
                Position(point),
                Vector3.one * 0.65f,
                Vector3.zero,
                shapeLifetime);
        }

        private Vector3 Position(Vector2 worldPoint)
        {
            return new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
        }
    }
}
