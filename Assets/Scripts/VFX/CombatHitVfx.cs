using ThinkFast.Combat;
using ThinkFast.Economy;
using ThinkFast.Enemy;
using ThinkFast.Player;
using UnityEngine;

namespace ThinkFast.VFX
{
    /// <summary>
    /// Listens for landed hits on either fighter and drives the scene
    /// <see cref="HitEffect"/> singleton plus a one-shot impact sound. Player hits
    /// use the strong burst while <see cref="FighterResources.IsFlowActive"/> is true.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CombatHitVfx : MonoBehaviour
    {
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private FighterResources playerResources;
        [SerializeField] private EnemyAttack enemyAttack;

        [Header("Audio")]
        [SerializeField] private AudioClip defaultClip;
        [SerializeField] private AudioClip strongClip;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

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

        private void PlayEffect(Vector2 point, float depth, bool strong)
        {
            var position = new Vector3(point.x, point.y, depth);

            if (HitEffect.Instance != null)
            {
                HitEffect.Instance.PlayHitEffect(position, strong);
            }

            AudioClip clip;
            if (strong)
            {
                clip = strongClip;
            }
            else
            {
                clip = defaultClip;
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
