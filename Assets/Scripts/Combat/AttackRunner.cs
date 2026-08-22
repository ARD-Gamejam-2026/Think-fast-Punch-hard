using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// One swing: the startup -> active -> recovery machine plus the hitbox
    /// sweep that goes with it.
    ///
    /// Plain C# rather than a MonoBehaviour, because both fighters own one. The
    /// player and the AI disagree about almost everything -- input, buffering,
    /// Action Points, when a swing is even a good idea -- but they must agree
    /// exactly on what an <see cref="AttackDefinition"/> MEANS, or frame-data
    /// bugs have to be found and fixed twice.
    ///
    /// What lives here: phase timing, the hitbox, one-hit-per-target, the
    /// self-hit skip, and the per-phase movement lock. What deliberately does
    /// not: anything about who decided to swing, or what it cost them.
    /// </summary>
    public sealed class AttackRunner
    {
        public enum Phase
        {
            Ready,
            Startup,
            Active,
            Recovery,
        }

        private readonly GameObject owner;
        private readonly Transform ownerTransform;
        private readonly ContactFilter2D hitFilter;

        // Reused so a swing allocates nothing.
        private readonly Collider2D[] hitResults = new Collider2D[16];

        // One hit per target per swing. Cleared when a new swing starts.
        private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        private float phaseTimer;

        // Cached from the last Tick so the hitbox and the gizmo agree between
        // steps. Facing is locked during startup and active anyway, so this can
        // never drift while it matters.
        private float facing = 1f;

        // Set by Begin, cleared by the very next Tick. A swing decided partway
        // through a physics step must not immediately lose a whole step of its
        // startup to that same step -- the window an opponent gets to react in is
        // the most sensitive number in the whole attack.
        private bool startedThisStep;

        public AttackRunner(GameObject owner, LayerMask hittableLayers)
        {
            this.owner = owner;
            ownerTransform = owner.transform;

            hitFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = hittableLayers,

                // Hitboxes are volumes, not solids: a trigger collider on the
                // target is a perfectly good thing to punch.
                useTriggers = true,
            };
        }

        /// <summary>
        /// Raised the instant a swing is committed to, at the start of startup,
        /// with the attack and where its hitbox is going to appear.
        ///
        /// This is the telegraph. Startup is by far the longest phase of an
        /// attack, and with nothing drawn during it the fighter appears to simply
        /// stop for a moment and then hit you -- which reads as lag, not as a
        /// wind-up. Something has to be shown here.
        /// </summary>
        public event Action<AttackDefinition, Vector2> Started;

        /// <summary>
        /// Raised the instant the hitbox opens, with the attack and the hitbox
        /// centre in world space. Presentation hangs off this rather than being
        /// baked in, so effects can be added or deleted without touching combat.
        /// </summary>
        public event Action<AttackDefinition, Vector2> BecameActive;

        /// <summary>Raised once per target actually hit, with the contact point.</summary>
        public event Action<AttackDefinition, Vector2> HitLanded;

        /// <summary>
        /// Scales the damage of anything landed from here on. The owner sets it;
        /// a fighter with no Flow economy simply leaves it at 1.
        /// </summary>
        public float DamageMultiplier { get; set; } = 1f;

        /// <summary>Scales the knockback of anything landed from here on.</summary>
        public float KnockbackMultiplier { get; set; } = 1f;

        public Phase CurrentPhase { get; private set; } = Phase.Ready;

        /// <summary>The attack in progress, or null when idle.</summary>
        public AttackDefinition Current { get; private set; }

        public bool IsAttacking => CurrentPhase != Phase.Ready;

        /// <summary>Name of the attack in progress, or empty when idle. Debug readout only.</summary>
        public string CurrentAttackName => Current != null ? Current.displayName : string.Empty;

        /// <summary>
        /// How much horizontal control the owner should have this step, 0 to 1.
        ///
        /// The lock is per-phase on purpose. Startup and active are the
        /// commitment; recovery only has to prevent another ATTACK, not prevent
        /// walking. A flat lock across all three leaves you frozen while
        /// whatever you just hit sails out of range.
        /// </summary>
        public float MoveControlScale => CurrentPhase switch
        {
            Phase.Startup or Phase.Active => Current.moveControlScale,
            Phase.Recovery => Current.recoveryMoveControlScale,
            _ => 1f,
        };

        /// <summary>
        /// True while facing must not change, so the hitbox cannot be flipped to
        /// the other side mid-swing. Frees up in recovery: the hitbox is long
        /// gone by then, so turning around cannot be abused.
        /// </summary>
        public bool LocksFacing => CurrentPhase == Phase.Startup || CurrentPhase == Phase.Active;

        /// <summary>Centre of the hitbox in world space, using the last known facing.</summary>
        public Vector2 HitboxCentre
        {
            get
            {
                if (Current == null)
                {
                    return ownerTransform.position;
                }

                Vector2 offset = Current.hitboxOffset;
                return (Vector2)ownerTransform.position + new Vector2(offset.x * facing, offset.y);
            }
        }

        /// <summary>
        /// Starts a swing. Callers decide whether one is allowed -- this will
        /// happily interrupt an attack already running.
        /// </summary>
        /// <param name="facingSign">
        /// Which way the swing points. Passed explicitly because the telegraph
        /// fires immediately, before the first Tick has had a chance to say.
        /// </param>
        public void Begin(AttackDefinition attack, float facingSign)
        {
            facing = facingSign;
            Current = attack;
            alreadyHit.Clear();
            CurrentPhase = Phase.Startup;
            phaseTimer = attack.startup;
            startedThisStep = true;

            Started?.Invoke(attack, HitboxCentre);
        }

        /// <summary>
        /// Drops the swing immediately. Nothing is refunded -- getting hit out of
        /// a punch is supposed to cost you.
        /// </summary>
        public void Cancel()
        {
            CurrentPhase = Phase.Ready;
            Current = null;
            phaseTimer = 0f;
            startedThisStep = false;
            alreadyHit.Clear();
        }

        /// <summary>
        /// Advances one physics step. <paramref name="facingSign"/> is -1 or +1
        /// and positions the hitbox.
        /// </summary>
        public void Tick(float dt, float facingSign)
        {
            facing = facingSign;

            if (CurrentPhase == Phase.Ready)
            {
                return;
            }

            if (startedThisStep)
            {
                startedThisStep = false;
                return;
            }

            phaseTimer -= dt;

            switch (CurrentPhase)
            {
                case Phase.Startup:
                    if (phaseTimer <= 0f)
                    {
                        CurrentPhase = Phase.Active;
                        phaseTimer = Current.active;
                        BecameActive?.Invoke(Current, HitboxCentre);
                        CheckForHits();
                    }
                    break;

                case Phase.Active:
                    // Sweep every step the hitbox is live, so a target moving
                    // into it mid-window is still caught.
                    CheckForHits();
                    if (phaseTimer <= 0f)
                    {
                        CurrentPhase = Phase.Recovery;
                        phaseTimer = Current.recovery;
                    }
                    break;

                case Phase.Recovery:
                    if (phaseTimer <= 0f)
                    {
                        CurrentPhase = Phase.Ready;
                        Current = null;
                    }
                    break;
            }
        }

        private void CheckForHits()
        {
            Vector2 centre = HitboxCentre;
            int count = Physics2D.OverlapBox(centre, Current.hitboxSize, 0f, hitFilter, hitResults);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = hitResults[i];
                if (hit == null)
                {
                    continue;
                }

                // Never punch yourself. No physics layers are in use, so the
                // hierarchy is the only thing separating attacker from target.
                if (hit.transform.IsChildOf(ownerTransform))
                {
                    continue;
                }

                var target = hit.GetComponentInParent<IDamageable>();
                if (target == null || !alreadyHit.Add(target))
                {
                    continue;
                }

                // Damage and knockback scale independently, so a hit can be made
                // to LOOK dramatically bigger without being balanced purely
                // around the damage number.
                var knockback = new Vector2(
                    Current.knockback.x * facing,
                    Current.knockback.y) * KnockbackMultiplier;

                int damage = Mathf.Max(1, Mathf.RoundToInt(Current.damage * DamageMultiplier));

                target.TakeHit(new HitInfo(damage, knockback, Current.hitstun, owner));
                HitLanded?.Invoke(Current, hit.ClosestPoint(centre));
            }
        }

        /// <summary>
        /// Draws the current hitbox. Call from the owner's OnDrawGizmos. Only the
        /// active window draws filled: that is the part that can actually hit
        /// something.
        /// </summary>
        public void DrawGizmos()
        {
            if (Current == null)
            {
                return;
            }

            Color colour = CurrentPhase switch
            {
                Phase.Startup => new Color(1f, 0.85f, 0.2f, 0.5f),
                Phase.Active => new Color(1f, 0.2f, 0.2f, 0.9f),
                Phase.Recovery => new Color(0.4f, 0.4f, 0.4f, 0.35f),
                _ => Color.clear,
            };

            if (colour == Color.clear)
            {
                return;
            }

            Gizmos.color = colour;
            var centre = (Vector3)HitboxCentre;
            centre.z = ownerTransform.position.z;
            var size = new Vector3(Current.hitboxSize.x, Current.hitboxSize.y, 0.2f);

            if (CurrentPhase == Phase.Active)
            {
                Gizmos.DrawCube(centre, size);
            }
            else
            {
                Gizmos.DrawWireCube(centre, size);
            }
        }
    }
}
