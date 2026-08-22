using ThinkFast.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThinkFast.Player
{
    /// <summary>
    /// THROWAWAY. Applies a canned hit to the fighter so damage, hitstun and
    /// knockback can be felt before any opponent exists that could deal them.
    ///
    /// Lives with the player rather than in Combat because it reads
    /// <see cref="PlayerController.Facing"/> -- and because the direction a
    /// simulated hit comes from is a fighter concern, not a health concern.
    ///
    /// Delete once the AI opponent can hit back.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    public sealed class DebugSelfDamage : MonoBehaviour
    {
        [SerializeField] private int damage = 10;

        [Tooltip("Knockback for the simulated hit. X is applied BACKWARDS relative to facing, as though the blow landed on the front of the fighter.")]
        [SerializeField] private Vector2 knockback = new Vector2(9f, 5f);

        [SerializeField] private float hitstun = 0.4f;

        private Health health;
        private PlayerController controller;

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.hKey.wasPressedThisFrame)
            {
                return;
            }

            // Knocked back the way you are looking, i.e. hit in the face.
            //
            // The previous rule pushed away from stage centre, which sent you
            // INTO an opponent whenever you stood between them and the middle --
            // precisely where a fight actually happens.
            var launch = new Vector2(knockback.x * -controller.Facing, knockback.y);

            health.TakeHit(new HitInfo(damage, launch, hitstun, gameObject));
        }
    }
}
