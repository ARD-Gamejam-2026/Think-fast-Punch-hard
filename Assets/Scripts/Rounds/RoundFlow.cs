using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.Rounds
{
    /// <summary>
    /// Turns the end of a fight into the end of a *round*: records the outcome
    /// and moves the game to the end screen.
    ///
    /// This is the real subscriber `RoundEvents` was written for, replacing the
    /// throwaway banner. Combat still reports through the same static seam and
    /// knows nothing about scenes.
    ///
    /// It also **reopens the round when a fight starts**, which is the part that
    /// is easy to miss. `RoundEvents.IsRoundOver` is static and a scene load does
    /// not clear it, so without this the second fight of a session would begin
    /// already over: the first `ReportRoundEnded` would be dropped as a duplicate
    /// and the round would never end again. That is invisible in a single fight
    /// and breaks the moment there is a menu to come back from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoundFlow : MonoBehaviour
    {
        [Tooltip("Scene loaded once the round is over. Must be in Build Settings.")]
        [SerializeField] private string endSceneName = "Scene_End";

        [Tooltip("Seconds between the round being reported over and the end screen loading. This is on TOP of the knockout's own delay, and is the beat where the fight is allowed to sit finished before it cuts away.")]
        [SerializeField, Min(0f)] private float transitionDelay = 1f;

        private float transitionTimer;
        private bool transitioning;

        private void Awake()
        {
            // Before any subscription, and before a knockout could possibly be
            // reported: whatever the previous fight left behind is cleared here.
            RoundEvents.ResetRound();
            MatchResult.Clear();
        }

        private void OnEnable()
        {
            RoundEvents.RoundEnded += HandleRoundEnded;
        }

        private void OnDisable()
        {
            RoundEvents.RoundEnded -= HandleRoundEnded;
        }

        private void HandleRoundEnded(RoundOutcome outcome)
        {
            if (transitioning)
            {
                return;
            }

            // Recorded immediately rather than when the scene actually loads, so
            // the outcome cannot be lost if something else ends the fight first.
            MatchResult.Record(outcome);

            transitioning = true;
            transitionTimer = transitionDelay;
        }

        private void Update()
        {
            if (!transitioning)
            {
                return;
            }

            transitionTimer -= Time.deltaTime;
            if (transitionTimer > 0f)
            {
                return;
            }

            transitioning = false;
            LoadEndScene();
        }

        private void LoadEndScene()
        {
            if (string.IsNullOrWhiteSpace(endSceneName))
            {
                Debug.LogError("RoundFlow has no end scene name, so the round ends with nothing to show for it.", this);
                return;
            }

            SceneManager.LoadScene(endSceneName);
        }
    }
}
