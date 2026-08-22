using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// What every fighter's body can do, regardless of whether a keyboard or an
    /// AI is driving it.
    ///
    /// Implemented by <see cref="ThinkFast.Player.PlayerController"/> and
    /// <see cref="ThinkFast.Enemy.EnemyMotor"/>. Both are far bigger than this --
    /// one reads input and handles fast-fall and drop-through, the other exposes
    /// jump-distance maths for route planning. Only what a *third party* needs to
    /// know about a fighter belongs here.
    ///
    /// The point of the interface is not that both classes happen to share these
    /// members; it is that code can be written against a fighter without knowing
    /// which one it has. <see cref="FighterHitReaction"/> is the proof: one
    /// component knocks either fighter around, because it never asks which it is.
    ///
    /// Note that it is deliberately not <c>IFighter</c>. Health, attacks and
    /// knockouts are separate components with separate contracts, and folding
    /// them all in here would produce an interface nothing could implement
    /// without becoming everything.
    /// </summary>
    public interface IFighterMotor
    {
        /// <summary>True while the feet probe is touching something solid.</summary>
        bool IsGrounded { get; }

        /// <summary>True while hitstun has taken control away.</summary>
        bool IsStunned { get; }

        /// <summary>Current velocity of the body.</summary>
        Vector2 Velocity { get; }

        /// <summary>-1 facing left, +1 facing right. Never 0.</summary>
        float Facing { get; }

        /// <summary>
        /// How far a jump rises, in units. What makes reachability answerable:
        /// the AI derives what it can climb from this rather than assuming a
        /// number, so re-tuning a jump re-tunes what its owner believes it can
        /// get to.
        /// </summary>
        float JumpApexHeight { get; }

        /// <summary>
        /// Scales horizontal control, 0 to 1. Set by whatever owns the fighter's
        /// attack, to root it during a swing. Restored every step by whoever
        /// lowered it, so a dropped state cannot leave a fighter stuck.
        /// </summary>
        float MoveControlScale { get; set; }

        /// <summary>
        /// While true, facing stops following anything. Set during a swing so the
        /// hitbox cannot be flipped to the other side mid-attack.
        /// </summary>
        bool FacingLocked { get; set; }

        /// <summary>
        /// Takes control away for a moment after being hit. Implementations must
        /// extend rather than replace an existing stun, so a second hit landing
        /// during the first cannot accidentally shorten it.
        /// </summary>
        void ApplyStun(float duration);
    }
}
