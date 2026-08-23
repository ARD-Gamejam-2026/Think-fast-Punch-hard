namespace ThinkFast.Tutorial
{
    /// <summary>
    /// What a tutorial step is waiting for before it lets the player move on.
    ///
    /// Kept as data rather than as a class per step: every step is "show this
    /// text, watch one number" and the interesting part is the text, which is
    /// far easier to read and re-word when the whole script sits in one table.
    /// </summary>
    public enum TutorialGoal
    {
        /// <summary>Nothing to do but read it. Advances on the button.</summary>
        Continue,

        /// <summary>Hold left and hold right, for a moment each.</summary>
        RunBothWays,

        /// <summary>Leave the ground under your own power, a few times.</summary>
        Jump,

        /// <summary>Connect with the training dummy.</summary>
        LandHits,

        /// <summary>Swing with an empty AP bar and be told no.</summary>
        SwingWithoutPoints,

        /// <summary>Get a quiz question right, however long it took.</summary>
        AnswerCorrectly,

        /// <summary>Get one right while the timer bar is still green.</summary>
        AnswerInTheGreen,

        /// <summary>Fill the Flow meter and enter Flow state.</summary>
        ReachFlowState,

        /// <summary>The last card. Its buttons leave the tutorial.</summary>
        Finish,
    }
}
