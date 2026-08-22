using ThinkFast.Quiz;
using UnityEngine;

namespace ThinkFast.Economy
{
    /// <summary>
    /// Bridges a resolved quiz question into fighter resources. This is the one
    /// object that knows about both halves of the game.
    ///
    /// It listens to <see cref="QuizController.QuestionResolved"/> -- the event
    /// that fires the instant an answer lands, before the feedback colours have
    /// been shown -- so the reward arrives while the player is still looking at
    /// the answer they just got right. Everything after that goes through
    /// <see cref="RiddleRewards"/>, which means neither half holds a reference
    /// to the other and deleting this component leaves both runnable alone.
    ///
    /// The rule it encodes: a correct answer is always worth one Action Point,
    /// but only a FAST one is worth Flow. Attacking is what solving buys you;
    /// the Flow burst is what solving *quickly* buys you, so grinding out
    /// correct-but-slow answers keeps you swinging and never builds the burst.
    /// Wrong answers and timeouts cost nothing beyond the Flow that drained
    /// while they were being got wrong.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuizRewardBridge : MonoBehaviour
    {
        [Header("Quiz")]
        [Tooltip("Left empty, the bridge looks for a QuizController on this object and then anywhere in the scene.")]
        [SerializeField] private QuizController quiz;

        [Tooltip("The view whose timer bar the player is watching. Its green zone IS the fast-solve window, so the threshold is read back from it instead of being duplicated here -- a green bar that does not pay out would be a lie.")]
        [SerializeField] private QuizView view;

        [Header("Reward")]
        [Tooltip("Flow granted by a solve inside the green zone. Slow-but-correct solves grant none. Tuned against a 6/s drain: roughly eight fast solves in a row reach the 100 needed for Flow state.")]
        [SerializeField, Min(0f)] private float flowPerFastSolve = 25f;

        [Tooltip("Fast-solve threshold used only when no view is assigned, as normalized time remaining. Keep it equal to QuizView's fast zone.")]
        [SerializeField, Range(0f, 1f)] private float fallbackFastZone = 0.6f;

        [Header("Debug")]
        [Tooltip("Logs every solve and what it paid. Off by default: the HUD already shows the result, and the quiz runs continuously.")]
        [SerializeField] private bool logSolves;

        /// <summary>
        /// Normalized time remaining a solve has to beat to earn Flow. Read from
        /// the view when there is one, so the payout and the green bar can never
        /// disagree.
        /// </summary>
        public float FastZoneThreshold
        {
            get
            {
                if (view != null)
                {
                    return view.FastZoneNormalized;
                }

                return fallbackFastZone;
            }
        }

        private void Awake()
        {
            if (quiz == null)
            {
                quiz = GetComponent<QuizController>();
            }

            if (quiz == null)
            {
                quiz = FindAnyObjectByType<QuizController>();
            }

            if (view == null && quiz != null)
            {
                view = quiz.GetComponentInChildren<QuizView>();
            }
        }

        private void OnEnable()
        {
            if (quiz == null)
            {
                Debug.LogError("QuizRewardBridge has no QuizController, so solves will not reward the fighter.", this);
                return;
            }

            // QuestionResolved, not QuestionAnswered: the second one waits out
            // the feedback delay, which would hold the Action Point back past
            // the moment it was earned.
            quiz.QuestionResolved += OnQuestionResolved;
        }

        private void OnDisable()
        {
            if (quiz != null)
            {
                quiz.QuestionResolved -= OnQuestionResolved;
            }
        }

        private void OnQuestionResolved(QuizResult result, float speed)
        {
            if (result != QuizResult.Correct)
            {
                return;
            }

            float flowReward = 0f;

            // Strictly greater, matching how QuizView decides the bar is still
            // green. Paying out on a bar the player saw turn yellow is exactly
            // the mismatch reading the threshold off the view is meant to avoid.
            if (speed > FastZoneThreshold)
            {
                flowReward = flowPerFastSolve;
            }

            // The static seam does the rest: one Action Point always, and the
            // Flow only if the fighter is currently accepting any.
            RiddleRewards.GrantSolve(flowReward);

            if (logSolves)
            {
                Debug.Log($"[QuizRewardBridge] correct at speed {speed:0.00} (fast above {FastZoneThreshold:0.00}): +1 AP, +{flowReward} flow requested.", this);
            }
        }
    }
}
