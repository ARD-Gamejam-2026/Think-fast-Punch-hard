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
        [SerializeField] private bool showBanner = true;

        [Tooltip("Draws the banner inside the fighter camera's viewport instead of across the whole window. Under split screen a full-width banner lands across the seam and half of it sits over the quiz.")]
        [SerializeField] private bool confineToFighterViewport = true;

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

            Rect viewport = FighterViewport();

            var centre = new Rect(viewport.x, viewport.y + (viewport.height * 0.35f), viewport.width, 70f);
            GUI.Label(centre, $"<color={colour}>{text}</color>", headline);
            GUI.Label(
                new Rect(viewport.x, centre.yMax, viewport.width, 30f),
                $"<color=#cccccc>press {restartKey} to fight again</color>",
                subline);
        }

        /// <summary>
        /// The part of the window the fight is drawn in, in GUI coordinates.
        /// Falls back to the whole window when there is no camera to ask, which
        /// is also the right answer when nothing has split the screen.
        /// </summary>
        private Rect FighterViewport()
        {
            var whole = new Rect(0f, 0f, Screen.width, Screen.height);
            if (!confineToFighterViewport)
            {
                return whole;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return whole;
            }

            // GUI measures Y down from the top, the camera measures it up from
            // the bottom, so the rect has to be flipped rather than copied.
            Rect pixels = camera.pixelRect;
            return new Rect(pixels.x, Screen.height - pixels.yMax, pixels.width, pixels.height);
        }
    }
}
