using ThinkFast.Quiz;
using ThinkFast.Rounds;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Feeds the quiz result and the round outcome into <see cref="MatchStats"/>.
    /// The one object that knows about both the quiz and the round, mirroring
    /// <c>QuizRewardBridge</c>; it starts the stats on <see cref="Awake"/> and
    /// finishes them when the round ends.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchStatsCoordinator : MonoBehaviour
    {
        [Tooltip("Left empty, the coordinator finds a QuizController in the scene.")]
        [SerializeField] private QuizController quiz;

        private void Awake()
        {
            MatchStats.Begin();
            if (quiz == null)
            {
                quiz = FindAnyObjectByType<QuizController>();
            }
        }

        private void OnEnable()
        {
            if (quiz != null)
            {
                quiz.QuestionResolved += HandleQuestionResolved;
            }

            RoundEvents.RoundEnded += HandleRoundEnded;
        }

        private void OnDisable()
        {
            if (quiz != null)
            {
                quiz.QuestionResolved -= HandleQuestionResolved;
            }

            RoundEvents.RoundEnded -= HandleRoundEnded;
        }

        private void HandleQuestionResolved(QuizResult result, float speed)
        {
            switch (result)
            {
                case QuizResult.Correct:
                    MatchStats.RecordCorrect();
                    break;
                case QuizResult.Wrong:
                    MatchStats.RecordWrong();
                    break;
                case QuizResult.TimedOut:
                    MatchStats.RecordTimedOut();
                    break;
                default:
                    break;
            }
        }

        private void HandleRoundEnded(RoundOutcome outcome)
        {
            bool playerWon = outcome == RoundOutcome.PlayerWon;
            MatchStats.Finish(playerWon);
        }
    }
}
