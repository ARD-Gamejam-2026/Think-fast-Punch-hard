using System;
using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// Hit points and nothing else. Deliberately knows nothing about knockback,
    /// stun, animation or UI -- it raises events and lets other components decide
    /// how this particular fighter reacts.
    ///
    /// Written to be shared: the player uses it now, the AI opponent will use the
    /// same component later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 100;

        [Tooltip("Brief window after being hit during which further hits are ignored. Stops a single lingering hitbox from draining the whole bar.")]
        [SerializeField] private float invulnerabilitySeconds = 0.15f;

        private float invulnerableUntil;

        public int Max => maxHealth;

        public int Current { get; private set; }

        /// <summary>0..1, for bars.</summary>
        public float Normalised => maxHealth > 0 ? Mathf.Clamp01((float)Current / maxHealth) : 0f;

        public bool IsDead => Current <= 0;

        /// <summary>Current, max. Raised on any change including a reset.</summary>
        public event Action<int, int> Changed;

        /// <summary>Raised when a hit is actually taken, carrying the full hit so reactions can read knockback and stun.</summary>
        public event Action<HitInfo> Hit;

        public event Action Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void Start()
        {
            // Announced in Start rather than Awake so listeners created in the
            // same frame have had a chance to subscribe.
            Changed?.Invoke(Current, maxHealth);
        }

        public void TakeHit(in HitInfo hit)
        {
            if (IsDead || Time.time < invulnerableUntil)
            {
                return;
            }

            invulnerableUntil = Time.time + invulnerabilitySeconds;

            Current = Mathf.Max(0, Current - hit.Damage);
            Changed?.Invoke(Current, maxHealth);

            // Raised even on a killing blow: knockback and hitstun should still
            // play out on the hit that finished you.
            Hit?.Invoke(hit);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            invulnerableUntil = 0f;
            Changed?.Invoke(Current, maxHealth);
        }
    }
}
