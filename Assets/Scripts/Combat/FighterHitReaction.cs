using ThinkFast.Anim;
using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// Turns a landed hit into knockback, hitstun and (optionally) a flash, on
    /// either fighter.
    ///
    /// This replaces a near-identical pair of components, one per fighter. It can
    /// be one component because it works against <see cref="IFighterMotor"/> and
    /// never asks whose body it is attached to -- which is the entire argument
    /// for that interface existing.
    ///
    /// Still split from <see cref="Health"/>, for the reason that has not
    /// changed: health is a number, but how a body reacts to being hit is
    /// specific to that body. What turned out NOT to be specific was knockback
    /// and stun. Those are the same for everyone; only the tuning differs, and
    /// tuning is what serialized fields are for.
    ///
    /// Dying is deliberately not handled here -- see
    /// <see cref="IFighterKnockout"/>, because losing a round is not a property
    /// of a body either.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FighterHitReaction : MonoBehaviour
    {
        [Tooltip("Multiplies incoming knockback. Below 1 makes this fighter heavy and hard to move.")]
        [SerializeField] private float knockbackScale = 1f;

        [Tooltip("Multiplies incoming hitstun. The real difficulty dial: longer stun means one hit leads into the next.")]
        [SerializeField] private float hitstunScale = 1f;

        [Header("Flash")]
        [Tooltip("Tint the renderers on hit. Off by default because anything ELSE that tints the same renderers will fight it -- the player already has a Flow-state tint, and two property-block writers on one renderer is a coin toss.")]
        [SerializeField] private bool flashOnHit;

        [Tooltip("Flashed for exactly as long as the hitstun lasts, so how long this fighter is helpless is legible without a debug readout.")]
        [SerializeField] private Color hitFlashColour = new Color(1f, 0.35f, 0.35f);

        [Tooltip("Held from the knockout onward, so a corpse does not look like a fighter having a rest.")]
        [SerializeField] private Color knockedOutColour = new Color(0.32f, 0.30f, 0.34f);

        [Tooltip("Left empty, every renderer in the hierarchy is tinted.")]
        [SerializeField] private Renderer[] renderers;

        [Header("Animation")]
        [Tooltip("Optional. Mesh Animator driver on a child object.")]
        [SerializeField] private CharacterAnimation characterAnimation;

        private Health health;
        private IFighterMotor motor;
        private IFighterKnockout knockout;
        private Rigidbody2D body;
        private RendererTint tint;
        private ICharacterAnimation animation;

        private float flashTimer;

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();

            // Interfaces cannot be declared with RequireComponent, so this is
            // checked by hand rather than by the editor.
            motor = GetComponent<IFighterMotor>();
            if (motor == null)
            {
                Debug.LogError($"{nameof(FighterHitReaction)} on '{name}' needs a component implementing {nameof(IFighterMotor)}.", this);
                enabled = false;
                return;
            }

            knockout = GetComponent<IFighterKnockout>();

            if (characterAnimation == null)
            {
                characterAnimation = GetComponentInChildren<CharacterAnimation>();
            }

            animation = characterAnimation;

            if (!flashOnHit)
            {
                return;
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            tint = new RendererTint(renderers);
        }

        private void OnEnable()
        {
            health.Hit += HandleHit;

            if (knockout != null)
            {
                knockout.KnockedOut += HandleKnockedOut;
                knockout.Revived += HandleRevived;
            }
        }

        private void OnDisable()
        {
            health.Hit -= HandleHit;

            if (knockout != null)
            {
                knockout.KnockedOut -= HandleKnockedOut;
                knockout.Revived -= HandleRevived;
            }
        }

        private void HandleHit(HitInfo hit)
        {
            // Assigned, not added, so a hit always launches by a predictable
            // amount regardless of what the fighter was doing at the time. Fixed
            // knockback has to look fixed.
            body.linearVelocity = hit.Knockback * knockbackScale;
            motor.ApplyStun(hit.Hitstun * hitstunScale);
            animation?.NotifyHit(!motor.IsGrounded);

            if (tint == null)
            {
                return;
            }

            flashTimer = hit.Hitstun * hitstunScale;
            tint.Set(hitFlashColour);
        }

        private void HandleKnockedOut()
        {
            flashTimer = 0f;
            tint?.Set(knockedOutColour);
            animation?.NotifyKnockout();
        }

        private void HandleRevived()
        {
            flashTimer = 0f;
            tint?.Clear();
            animation?.NotifyRespawn();
        }

        private void Update()
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                tint?.Clear();
            }
        }
    }
}
