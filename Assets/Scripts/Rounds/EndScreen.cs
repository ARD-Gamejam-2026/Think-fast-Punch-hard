using TMPro;
using UnityEngine;

namespace ThinkFast.Rounds
{
    /// <summary>
    /// Makes the end screen say which ending this was.
    ///
    /// One scene serves both outcomes rather than two near-identical scenes,
    /// because the difference between winning and losing here is a line of text
    /// and a colour -- and two scenes would mean every later change to the layout
    /// being made twice, with the loss screen quietly falling behind.
    ///
    /// Opened on its own with no fight behind it, it leaves the authored text
    /// alone instead of claiming a win.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EndScreen : MonoBehaviour
    {
        [Header("Message")]
        [Tooltip("The label the outcome is written into. Left empty, the authored text stays as it is.")]
        [SerializeField] private TMP_Text message;

        [Header("Won")]
        [SerializeField] private string winMessage = "You mastered your Body and Mind";

        [SerializeField] private Color winColour = new Color(1f, 0.82f, 0.29f);

        [Header("Lost")]
        [Tooltip("The mirror of the win line: the mind held up, the body did not.")]
        [SerializeField] private string loseMessage = "Your Mind was willing. Your Body was not.";

        [SerializeField] private Color loseColour = new Color(1f, 0.40f, 0.40f);

        private void Start()
        {
            if (message == null)
            {
                Debug.LogWarning("EndScreen has no message label, so the ending is not written anywhere.", this);
                return;
            }

            // No result means nobody fought -- the scene was opened directly, or
            // loaded out of order. Overwriting the authored text here would show
            // a win that never happened.
            if (!MatchResult.HasResult)
            {
                return;
            }

            if (MatchResult.Outcome == RoundOutcome.PlayerWon)
            {
                Apply(winMessage, winColour);
                return;
            }

            Apply(loseMessage, loseColour);
        }

        private void Apply(string text, Color colour)
        {
            message.text = text;
            message.color = colour;
        }
    }
}
