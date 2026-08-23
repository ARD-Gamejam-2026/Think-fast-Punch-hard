using System.Collections.Generic;

namespace ThinkFast.Tutorial
{
    /// <summary>
    /// Everything the tutorial says, in order.
    ///
    /// Separated from <see cref="TutorialDirector"/> because it is content, not
    /// behaviour: it is the part most likely to be re-worded, it should be
    /// readable without any of the machinery around it, and -- the reason it is
    /// static rather than built per instance -- it can then be measured against
    /// the card it has to fit inside without entering Play mode. See
    /// <c>Tools > Think Fast > Check Tutorial Text Fits</c>.
    ///
    /// Keep the bodies to about two lines. The card auto-shrinks type that
    /// overruns, which stops a re-wording from spilling off the bottom, but a
    /// step that shrinks is a step somebody has to squint at.
    /// </summary>
    public static class TutorialScript
    {
        /// <summary>Builds the step list. A fresh list each call; nothing here is shared state.</summary>
        public static List<TutorialStepDefinition> Build()
        {
            return new List<TutorialStepDefinition>
            {
                new TutorialStepDefinition
                {
                    Title = "Your Body and your Mind",
                    Body =
                        "Your <b>body</b> fights on the left. Your <b>mind</b> solves questions on the right. Only with a strong mind, your fighter can succeed",
                    Goal = TutorialGoal.Continue,
                    KeepsPointsTopped = true,
                },

                new TutorialStepDefinition
                {
                    Title = "Move",
                    Body =
                        "<b>A</b> and <b>D</b> run. The camera keeps you in frame.",
                    Objective = "Run both ways",
                    Goal = TutorialGoal.RunBothWays,
                    Target = 0.7f,
                    KeepsPointsTopped = true,
                },

                new TutorialStepDefinition
                {
                    Title = "Jump",
                    Body =
                        "<b>W</b> jumps - hold it longer to go higher. <b>S</b> drops you fast in the air, or " +
                        "straight through a ledge you are standing on.",
                    Objective = "Jump three times",
                    Goal = TutorialGoal.Jump,
                    Target = 3f,
                    KeepsPointsTopped = true,
                },

                new TutorialStepDefinition
                {
                    Title = "Punch",
                    Body =
                        "<b>Space</b> punches, and each one spends an <b>Action Point</b> - the pips you see in the bottom " +
                        "left. You can stack up to five by using your mind (see next step). For this step, they will auto-restore.",
                    Objective = "Land two hits on the trainer",
                    Goal = TutorialGoal.LandHits,
                    Target = 2f,
                    KeepsPointsTopped = true,
                },

                new TutorialStepDefinition
                {
                    Title = "Out of points",
                    Body =
                        "Practice is over - the pips stop refilling. From here on, only your mind can help you restore Action Points.",
                    Objective = "Press Space to continue",
                    Goal = TutorialGoal.SwingWithoutPoints,
                    MinimumSeconds = 1.5f,
                },

                new TutorialStepDefinition
                {
                    Title = "Solve riddles to arm yourself",
                    Body =
                        "Four answers, one right, one countdown. A correct answer pays <b>one Action Point</b> " +
                        "straight into your fists.",
                    Objective = "Get one question right",
                    Goal = TutorialGoal.AnswerCorrectly,
                    Target = 1f,
                    RevealsQuiz = true,
                },

                new TutorialStepDefinition
                {
                    Title = "Be fast to get into Flow State",
                    Body =
                        "While the timer bar is still <b>green</b>, a correct answer also puts you into <b>Flow State</b> - the " +
                        "meter above your pips.",
                    Objective = "Answer one inside the green",
                    Goal = TutorialGoal.AnswerInTheGreen,
                    Target = 1f,
                    MinimumSeconds = 2f,
                },

                new TutorialStepDefinition
                {
                    Title = "Flow state",
                    Body =
                        "Flow bleeds away on its own over time. Fill the meter completely, reach Flow state and your punches hit <b>twice as hard</b> - but it " +
                        "drains fast and every swing burns it extra",
                    Objective = "Fill the Flow meter",
                    Goal = TutorialGoal.ReachFlowState,
                    MinimumSeconds = 4f,
                    CanBeSkipped = true,
                },

                new TutorialStepDefinition
                {
                    Title = "How you win",
                    Body =
                        "The bars along the top are health - yours left, theirs right. Knock the other fighter out " +
                        "before they knock you out. Sounds easy, right?",
                    Goal = TutorialGoal.Continue,
                },

                new TutorialStepDefinition
                {
                    Title = "Think fast, punch hard",
                    Body =
                        "Sharpen the mind and the body follows. Keep answering, answer fast, reach flow state and beat your trainer!",
                    Goal = TutorialGoal.Finish,
                },
            };
        }
    }
}
