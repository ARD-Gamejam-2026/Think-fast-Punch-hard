using ThinkFast.Combat;
using ThinkFast.Economy;
using ThinkFast.Enemy;
using ThinkFast.Player;
using UnityEngine;

namespace ThinkFast.VFX
{
    /// <summary>
    /// Listens for landed hits on either fighter and drives the scene
    /// <see cref="HitEffect"/> singleton. Player hits use the strong burst while
    /// <see cref="FighterResources.IsFlowActive"/> is true.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitVfx : MonoBehaviour
    {
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private FighterResources playerResources;
        [SerializeField] private EnemyAttack enemyAttack;

        private void Awake()
        {
            if (playerAttack == null)
            {
                playerAttack = FindAnyObjectByType<PlayerAttack>();
            }

            if (playerResources == null && playerAttack != null)
            {
                playerResources = playerAttack.GetComponent<FighterResources>();
            }

            if (enemyAttack == null)
            {
                enemyAttack = FindAnyObjectByType<EnemyAttack>();
            }
        }

        private void OnEnable()
        {
            if (playerAttack != null)
            {
                playerAttack.HitLanded += HandlePlayerHitLanded;
            }

            if (enemyAttack != null)
            {
                enemyAttack.HitLanded += HandleEnemyHitLanded;
            }
        }

        private void OnDisable()
        {
            if (playerAttack != null)
            {
                playerAttack.HitLanded -= HandlePlayerHitLanded;
            }

            if (enemyAttack != null)
            {
                enemyAttack.HitLanded -= HandleEnemyHitLanded;
            }
        }

        private void HandlePlayerHitLanded(AttackDefinition definition, Vector2 point)
        {
            bool strong = playerResources != null && playerResources.IsFlowActive;
            PlayEffect(point, playerAttack.transform.position.z, strong);
        }

        private void HandleEnemyHitLanded(AttackDefinition definition, Vector2 point)
        {
            PlayEffect(point, enemyAttack.transform.position.z, false);
        }

        private static void PlayEffect(Vector2 point, float depth, bool strong)
        {
            if (HitEffect.Instance == null)
            {
                return;
            }

            var position = new Vector3(point.x, point.y, depth);
            HitEffect.Instance.PlayHitEffect(position, strong);
        }
    }
}
