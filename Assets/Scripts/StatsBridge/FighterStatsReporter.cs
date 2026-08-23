using ThinkFast.Combat;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>Which fighter a <see cref="FighterStatsReporter"/> reports for.</summary>
    public enum FighterRole
    {
        Player,
        Opponent,
    }

    /// <summary>
    /// Reports one fighter's health into <see cref="MatchStats"/>, so damage and
    /// end health are tracked without <see cref="Health"/> having to know about
    /// statistics. Put one on each fighter and set its role.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FighterStatsReporter : MonoBehaviour
    {
        [Tooltip("Whether this fighter is the player or the opponent.")]
        [SerializeField] private FighterRole role = FighterRole.Player;

        [Tooltip("The health this reporter watches. Left empty, it looks on this object.")]
        [SerializeField] private Health health;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (health == null)
            {
                Debug.LogError("FighterStatsReporter has no Health, so its stats will not be tracked.", this);
                return;
            }

            health.Changed += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (role == FighterRole.Player)
            {
                MatchStats.SetPlayerHealth(current);
            }
            else
            {
                MatchStats.SetOpponentHealth(current);
            }
        }
    }
}
