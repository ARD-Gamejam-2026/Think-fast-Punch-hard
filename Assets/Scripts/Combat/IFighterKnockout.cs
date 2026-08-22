using System;

namespace ThinkFast.Combat
{
    /// <summary>
    /// A fighter being knocked out and, for debugging, picked back up.
    ///
    /// Implemented by <see cref="ThinkFast.Player.PlayerKnockout"/> and
    /// <see cref="ThinkFast.Enemy.EnemyKnockout"/>. What those two do about a
    /// knockout differs -- they disable different components and report opposite
    /// round outcomes -- which is exactly why they are still separate classes.
    /// What they have in common is only that it *happened*, and that is all this
    /// says.
    ///
    /// It exists so presentation can react to a knockout without caring whose it
    /// was.
    /// </summary>
    public interface IFighterKnockout
    {
        /// <summary>Raised the moment health hits zero, before any round-end delay.</summary>
        event Action KnockedOut;

        /// <summary>Raised when the fighter is put back on its feet. Debug affordance only.</summary>
        event Action Revived;

        bool IsKnockedOut { get; }
    }
}
