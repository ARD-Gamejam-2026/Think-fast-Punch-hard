namespace ThinkFast.Player
{
    /// <summary>
    /// Presentation seam for the fighter mesh. Gameplay calls these when movement,
    /// attacks, hits or death happen; <see cref="CharacterAnimation"/> maps them
    /// onto the <see cref="UnityEngine.Animator"/> on the same GameObject.
    /// </summary>
    public interface ICharacterAnimation
    {
        /// <summary>Called the instant a jump is committed.</summary>
        void NotifyJump();

        /// <summary>Called when an attack swing begins.</summary>
        /// <param name="airborne">True when the swing started in the air.</param>
        void NotifyAttackStarted(bool airborne);

        /// <summary>Called when this fighter takes a hit.</summary>
        /// <param name="airborne">True when the fighter was not grounded on impact.</param>
        void NotifyHit(bool airborne);

        /// <summary>Called when HP reaches zero.</summary>
        void NotifyKnockout();

        /// <summary>Called when a knocked-out fighter respawns.</summary>
        void NotifyRespawn();
    }
}
