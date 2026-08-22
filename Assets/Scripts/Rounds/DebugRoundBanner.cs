using ThinkFast.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ThinkFast.Rounds
{
    /// <summary>
    /// Throwaway. Shows the round result in the middle of the screen and reloads
    /// the scene on a keypress, so a KO is visibly an ENDING rather than the
    /// fight quietly continuing against a corpse.
    ///
    /// Stands in for the real thing, which is a scene transition to a results
    /// screen. Delete this component and its script the moment that exists --
    /// nothing in combat references it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugRoundBanner : MonoBehaviour
    {
        [SplitScreenTodo("The banner is drawn against Screen.width/Screen.height, so with a split viewport it lands across the seam instead of over the fight. The real results screen replaces this, but if it outlives split screen it needs the fighter camera viewport rect applied.")]
        [SerializeField] private bool showBanner = true;

        [Tooltip("Reloads the active scene, restarting the fight from scratch.")]
        [SerializeField] private Key restartKey = Key.Enter;

        private RoundOutcome outcome;
        private bool roundOver;

        private void OnEnable()
        {
            RoundEvents.RoundEnded += HandleRoundEnded;
        }

        private void OnDisable()
        {
            RoundEvents.RoundEnded -= HandleRoundEnded;
        }

        private void HandleRoundEnded(RoundOutcome result)
        {
            outcome = result;
            roundOver = true;
            Debug.Log($"Round over: {result}.");
        }

        private void Update()
        {
            if (!roundOver || Keyboard.current == null)
            {
                return;
            }

            // Read straight from the device rather than through the action asset:
            // this is a testing convenience, not a game binding.
            if (Keyboard.current[restartKey].wasPressedThisFrame)
            {
                RoundEvents.ResetRound();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnGUI()
        {
            if (!roundOver || !showBanner)
            {
                return;
            }

            bool won = outcome == RoundOutcome.PlayerWon;

            var headline = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                richText = true,
            };

            var subline = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                richText = true,
            };

            string colour = won ? "#ffd24a" : "#ff6666";
            string text = won ? "K.O. -- YOU WIN" : "K.O. -- YOU LOSE";

            var centre = new Rect(0f, Screen.height * 0.35f, Screen.width, 70f);
            GUI.Label(centre, $"<color={colour}>{text}</color>", headline);
            GUI.Label(
                new Rect(0f, centre.yMax, Screen.width, 30f),
                $"<color=#cccccc>press {restartKey} to fight again</color>",
                subline);
        }
    }
}
