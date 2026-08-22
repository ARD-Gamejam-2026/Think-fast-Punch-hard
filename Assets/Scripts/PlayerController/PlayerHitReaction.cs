using ThinkFast.Combat;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// Turns a landed hit into knockback and hitstun on the fighter.
    ///
    /// Split from <see cref="Health"/> on purpose: health is just a number, while
    /// how a body reacts to being hit is very specific to the thing being hit.
    /// The AI opponent will share Health but is free to react differently.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerHitReaction : MonoBehaviour
    {
        [Tooltip("Multiplies incoming knockback. Below 1 makes this fighter heavy and hard to move.")]
        [SerializeField] private float knockbackScale = 1f;

        [Tooltip("Multiplies incoming hitstun. This is the real difficulty dial: long stun means one hit leads to another.")]
        [SerializeField] private float hitstunScale = 1f;

        [Header("Death")]
        [Tooltip("Seconds before respawning at the spawn point. Placeholder until a real round flow exists.")]
        [SerializeField] private float respawnDelay = 1.5f;

        private Health health;
        private PlayerController controller;
        private Rigidbody2D body;

        private Vector2 spawnPosition;
        private float respawnTimer;

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
        }

        private void OnEnable()
        {
            health.Hit += HandleHit;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Hit -= HandleHit;
            health.Died -= HandleDied;
        }

        private void HandleHit(HitInfo hit)
        {
            // Assigned, not added, so a hit always launches by a predictable
            // amount regardless of what the fighter was doing at the time.
            body.linearVelocity = hit.Knockback * knockbackScale;
            controller.ApplyStun(hit.Hitstun * hitstunScale);
        }

        private void HandleDied()
        {
            respawnTimer = respawnDelay;
        }

        private void Update()
        {
            if (respawnTimer <= 0f)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            transform.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            health.ResetHealth();
        }
    }
}
