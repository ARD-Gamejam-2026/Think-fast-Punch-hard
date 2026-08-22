using ThinkFast.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThinkFast.Enemy
{
    /// <summary>
    /// Throwaway. A floating readout above the opponent -- health, what it has
    /// decided to do, and what it is swinging -- plus a key to put it back on its
    /// feet.
    ///
    /// The state label is the point: without it, an AI that is standing still
    /// because it is spacing looks identical to one that is standing still
    /// because it lost its target. Delete this component and its script once the
    /// opponent behaves.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyDebugHud : MonoBehaviour
    {
        [Tooltip("Floating HP and AI state over the opponent. Split-screen safe: WorldToScreenPoint already accounts for the camera's viewport rect, so the label follows the opponent inside the fighter half rather than across the whole window.")]
        [SerializeField] private bool showLabel = true;

        [Tooltip("Puts the opponent back at its spawn point at full health, without leaving Play mode.")]
        [SerializeField] private Key resetKey = Key.R;

        [SerializeField] private float labelHeight = 1.6f;

        private Health health;
        private EnemyBrain brain;
        private EnemyAttack attack;
        private EnemyKnockout knockout;

        private void Awake()
        {
            health = GetComponent<Health>();
            brain = GetComponent<EnemyBrain>();
            attack = GetComponent<EnemyAttack>();
            knockout = GetComponent<EnemyKnockout>();
        }

        private void Update()
        {
            // Read straight from the device rather than through the action asset:
            // this is a testing convenience, not a game binding.
            if (knockout != null
                && Keyboard.current != null
                && Keyboard.current[resetKey].wasPressedThisFrame)
            {
                knockout.Revive();
            }
        }

        private void OnGUI()
        {
            if (!showLabel)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
            if (screen.z < 0f)
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                richText = true,
            };

            var rect = new Rect(screen.x - 90f, Screen.height - screen.y - 26f, 180f, 40f);
            GUI.Label(rect, BuildLabel(), style);
        }

        private string BuildLabel()
        {
            if (knockout != null && knockout.IsKnockedOut)
            {
                return $"<color=#ff6666>K.O.</color>\n<color=#888888>{resetKey} to reset</color>";
            }

            string hp = $"{health.Current} / {health.Max}";

            string status = brain != null ? brain.CurrentState.ToString() : "-";
            if (attack != null && attack.IsAttacking)
            {
                status = $"<color=#ffcc44>{attack.CurrentAttackName} : {attack.CurrentPhase}</color>";
            }
            else if (brain != null && brain.CurrentState == EnemyBrain.State.Stunned)
            {
                status = "<color=#ff6666>Stunned</color>";
            }

            return $"{hp}\n<color=#aaaaaa>{status}</color>";
        }
    }
}
