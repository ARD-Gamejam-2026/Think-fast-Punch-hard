namespace ThinkFast.Tutorial
{
    /// <summary>
    /// One card of the tutorial: what it says, and what it is waiting for.
    ///
    /// Plain data with no behaviour, so the whole script can be read top to
    /// bottom in <see cref="TutorialDirector"/> without chasing a class per
    /// step.
    /// </summary>
    public sealed class TutorialStepDefinition
    {
        /// <summary>Short heading, in the card's own voice.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>The teaching. Two or three lines; TMP rich text is allowed.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>What the player has to do, phrased as an instruction.</summary>
        public string Objective { get; set; } = string.Empty;

        /// <summary>What the director watches to decide the step is done.</summary>
        public TutorialGoal Goal { get; set; } = TutorialGoal.Continue;

        /// <summary>
        /// How much of the goal is needed: seconds held for
        /// <see cref="TutorialGoal.RunBothWays"/>, otherwise a count. Ignored by
        /// goals that are simply true or false.
        /// </summary>
        public float Target { get; set; } = 1f;

        /// <summary>
        /// Whether the button can carry the player past this step without
        /// meeting its goal.
        ///
        /// On for the two steps whose goal is a stretch -- Flow state needs
        /// several fast solves in a row -- so a tutorial can never become the
        /// thing the player is stuck inside.
        /// </summary>
        public bool CanBeSkipped { get; set; }

        /// <summary>
        /// Shortest time the card stays up before its goal can complete it.
        ///
        /// Steps whose goal may already be met the moment they appear -- Flow
        /// state, above all -- would otherwise flash past unread.
        /// </summary>
        public float MinimumSeconds { get; set; } = 0.75f;

        /// <summary>Set on the step that opens the quiz half.</summary>
        public bool RevealsQuiz { get; set; }

        /// <summary>
        /// Set on every step before running dry becomes the lesson.
        ///
        /// While it is on, the tutorial keeps the player stocked with Action
        /// Points. Scarcity belongs to exactly one step, and until then a punch
        /// that silently does nothing teaches the wrong thing about the punch
        /// key -- including during the welcome and movement steps, where players
        /// press Space long before anything asks them to.
        /// </summary>
        public bool KeepsPointsTopped { get; set; }
    }
}
