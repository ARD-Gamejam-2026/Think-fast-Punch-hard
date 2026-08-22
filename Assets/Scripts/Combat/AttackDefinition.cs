using System;
using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// The frame data and payload for a single attack. Two of these exist on the
    /// fighter -- one grounded, one airborne -- selected by the same button.
    ///
    /// Timings are in seconds rather than frames on purpose: there is no
    /// animation yet, and once there is, these get slaved to the clip lengths.
    /// Treat every number here as provisional.
    /// </summary>
    [Serializable]
    public sealed class AttackDefinition
    {
        [Tooltip("Shown in the debug readout so the two attacks are tellable apart.")]
        public string displayName = "Attack";

        [Header("Timing (seconds)")]
        [Tooltip("Wind-up before the hitbox exists. This is the window an opponent can react in, so it is most of what makes an attack feel committal.")]
        public float startup = 0.15f;

        [Tooltip("How long the hitbox is live.")]
        public float active = 0.06f;

        [Tooltip("Tail after the hitbox closes, during which you cannot act. This is what makes a whiff punishable.")]
        public float recovery = 0.21f;

        [Header("Payload")]
        public int damage = 10;

        [Tooltip("Launch velocity applied to whatever is hit. X is along the attacker's facing; Y is always up.")]
        public Vector2 knockback = new Vector2(11f, 4f);

        [Tooltip("How long the target is stunned. Long enough to read the hit, short enough not to feel like a freeze.")]
        public float hitstun = 0.4f;

        [Header("Hitbox")]
        [Tooltip("Offset from the fighter's centre, in local space. X is along facing.")]
        public Vector2 hitboxOffset = new Vector2(0.9f, 0.1f);

        public Vector2 hitboxSize = new Vector2(1.4f, 1.0f);

        [Header("Movement")]
        [Tooltip("Movement control retained during startup and active. 0 roots you in place, which is what makes the attack a commitment.")]
        [Range(0f, 1f)] public float moveControlScale;

        [Tooltip("Movement control retained during recovery. Recovery only needs to stop you ATTACKING again, not stop you walking -- keeping this high lets you chase what you just knocked away.")]
        [Range(0f, 1f)] public float recoveryMoveControlScale = 0.7f;

        /// <summary>Total time the fighter is locked into this attack.</summary>
        public float TotalDuration => startup + active + recovery;
    }
}
