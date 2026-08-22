using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// One landed hit, fully resolved into world space by the attacker. The
    /// receiver should not need to know who hit it or from where -- everything it
    /// needs to react is in here.
    /// </summary>
    public readonly struct HitInfo
    {
        /// <summary>HP to subtract.</summary>
        public readonly int Damage;

        /// <summary>World-space launch velocity, already flipped to match the attacker's facing.</summary>
        public readonly Vector2 Knockback;

        /// <summary>How long the receiver is stunned, in seconds.</summary>
        public readonly float Hitstun;

        /// <summary>Whoever threw the hit. Used to avoid self-hits and, later, to award AP.</summary>
        public readonly GameObject Source;

        public HitInfo(int damage, Vector2 knockback, float hitstun, GameObject source)
        {
            Damage = damage;
            Knockback = knockback;
            Hitstun = hitstun;
            Source = source;
        }
    }

    /// <summary>Anything that can be punched.</summary>
    public interface IDamageable
    {
        /// <summary>Called once per swing per target. Implementations must tolerate being called while already stunned.</summary>
        void TakeHit(in HitInfo hit);
    }
}
