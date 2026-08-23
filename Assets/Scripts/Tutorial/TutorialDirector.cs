using System.Collections.Generic;
using ThinkFast.Combat;
using ThinkFast.Economy;
using ThinkFast.Player;
using ThinkFast.Quiz;
using ThinkFast.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ThinkFast.Tutorial
{
    /// <summary>
    /// Walks a new player through both halves of the game, one card at a time:
    /// move, jump, punch, run out of Action Points, answer a question to get
    /// them back, answer fast enough to build Flow, and finally what winning
    /// looks like.
    ///
    /// It teaches by watching the real systems rather than by scripting them.
    /// Every goal is read off the components the fight already uses -- the
    /// controller, the attack, the resources, the quiz -- so nothing here can
    /// tell the player something the game does not actually do. The one thing
    /// it does reach in and change is the quiz, which stays shut until the step
    /// that introduces it.
    ///
    /// The tutorial deliberately owns no round: the opponent is a
    /// <see cref="TutorialDummy"/> that never fights back and never stays down,
    /// so there is no way to lose and no reason to hurry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialDirector : MonoBehaviour
    {
        [Header("Coach")]
        [SerializeField] private TutorialCoach coach;

        [Header("Fighter")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAttack attack;
        [SerializeField] private FighterResources resources;

        [Header("Quiz")]
        [Tooltip("Left shut until the step that introduces it, so the countdown is not ticking behind the movement lesson.")]
        [SerializeField] private GameObject quizPanel;

        [Tooltip("Stand-in shown in the quiz half until the quiz opens.")]
        [SerializeField] private GameObject quizPlaceholder;

        [SerializeField] private QuizController quiz;
        [SerializeField] private QuizView quizView;

        [Tooltip("Disabled at build time so it does not start asking questions on scene load; enabling it is what starts the quiz.")]
        [SerializeField] private QuizFlow quizFlow;

        [Tooltip("Re-applied once the quiz panel is shown, so the panel is scaled into its half the frame it appears.")]
        [SerializeField] private SplitScreenLayout layout;

        [Header("Exits")]
        [SerializeField] private string fightSceneName = "PlayerControllerTest";
        [SerializeField] private string menuSceneName = "Scene_Menu";

        /// <summary>Fast-solve threshold used when no view is available to read it from.</summary>
        private const float FallbackFastZone = 0.6f;

        /// <summary>
        /// Action Points the practice steps keep the player stocked to. Two,
        /// matching the fighter's own starting count, so the pip row looks the
        /// same as it will at the start of a real fight.
        /// </summary>
        private const int PracticeActionPoints = 2;

        /// <summary>Beat before a spent practice point comes back. Short enough that a swing outlasts it.</summary>
        private const float TopUpDelaySeconds = 0.4f;

        private readonly List<TutorialStepDefinition> steps = new();

        private int index;
        private float stepSeconds;
        private bool advanceRequested;

        private float leftHeldSeconds;
        private float rightHeldSeconds;
        private int jumps;
        private int hitsLanded;
        private bool swingRefused;
        private int correctAnswers;
        private int fastAnswers;
        private bool reachedFlowState;

        private bool wasGrounded;
        private float topUpSeconds;
        private bool quizRevealed;

        private TutorialStepDefinition Current => steps[index];

        private void Awake()
        {
            BuildSteps();
            HideQuiz();
        }

        private void OnEnable()
        {
            if (attack != null)
            {
                attack.HitLanded += HandleHitLanded;
                attack.AttackRefused += HandleAttackRefused;
            }

            if (resources != null)
            {
                resources.FlowStateEntered += HandleFlowStateEntered;
            }

            if (quiz != null)
            {
                quiz.QuestionResolved += HandleQuestionResolved;
            }

            if (coach != null)
            {
                coach.PrimaryPressed += HandlePrimaryPressed;
                coach.SecondaryPressed += HandleSecondaryPressed;
                coach.SkipPressed += HandleSkipPressed;
            }
        }

        private void OnDisable()
        {
            if (attack != null)
            {
                attack.HitLanded -= HandleHitLanded;
                attack.AttackRefused -= HandleAttackRefused;
            }

            if (resources != null)
            {
                resources.FlowStateEntered -= HandleFlowStateEntered;
            }

            if (quiz != null)
            {
                quiz.QuestionResolved -= HandleQuestionResolved;
            }

            if (coach != null)
            {
                coach.PrimaryPressed -= HandlePrimaryPressed;
                coach.SecondaryPressed -= HandleSecondaryPressed;
                coach.SkipPressed -= HandleSkipPressed;
            }
        }

        private void Start()
        {
            BeginStep(0);
        }

        private void Update()
        {
            stepSeconds += Time.deltaTime;

            TrackMovement();
            TrackJumps();
            TrackEnterKey();
            TopUpPointsIfNeeded();

            coach?.ShowProgress(HasProgressBar(Current.Goal), StepProgress());

            if (stepSeconds < Current.MinimumSeconds)
            {
                return;
            }

            if (IsStepSatisfied() || (advanceRequested && Current.CanBeSkipped))
            {
                Advance();
            }
        }

        /// <summary>
        /// Loads the script. The content itself lives in
        /// <see cref="TutorialScript"/> so it can be read -- and measured
        /// against the card it has to fit inside -- without this class.
        /// </summary>
        private void BuildSteps()
        {
            steps.Clear();
            steps.AddRange(TutorialScript.Build());
        }

        private void BeginStep(int next)
        {
            index = Mathf.Clamp(next, 0, steps.Count - 1);
            stepSeconds = 0f;
            advanceRequested = false;

            ResetGoalCounters();

            TutorialStepDefinition step = Current;

            if (step.RevealsQuiz)
            {
                RevealQuiz();
            }

            if (step.Goal == TutorialGoal.SwingWithoutPoints)
            {
                EmptyActionPoints();
            }

            coach?.ShowStep($"STEP {index + 1} OF {steps.Count}", step.Title, step.Body, step.Objective);
            coach?.ShowProgress(HasProgressBar(step.Goal), 0f);
            ShowButtonsFor(step);
        }

        private void Advance()
        {
            if (index + 1 >= steps.Count)
            {
                return;
            }

            BeginStep(index + 1);
        }

        private void ShowButtonsFor(TutorialStepDefinition step)
        {
            if (coach == null)
            {
                return;
            }

            switch (step.Goal)
            {
                case TutorialGoal.Finish:
                    coach.ShowPrimary(true, "START THE FIGHT");
                    coach.ShowSecondary(true, "MAIN MENU");
                    break;

                case TutorialGoal.Continue:
                    coach.ShowPrimary(true, "CONTINUE");
                    coach.ShowSecondary(false, string.Empty);
                    break;

                default:
                    coach.ShowPrimary(step.CanBeSkipped, "SKIP THIS STEP");
                    coach.ShowSecondary(false, string.Empty);
                    break;
            }
        }

        private bool IsStepSatisfied()
        {
            TutorialStepDefinition step = Current;

            switch (step.Goal)
            {
                case TutorialGoal.Continue:
                    return advanceRequested;

                case TutorialGoal.RunBothWays:
                    return leftHeldSeconds >= step.Target && rightHeldSeconds >= step.Target;

                case TutorialGoal.Jump:
                    return jumps >= step.Target;

                case TutorialGoal.LandHits:
                    return hitsLanded >= step.Target;

                case TutorialGoal.SwingWithoutPoints:
                    return swingRefused;

                case TutorialGoal.AnswerCorrectly:
                    return correctAnswers >= step.Target;

                case TutorialGoal.AnswerInTheGreen:
                    return fastAnswers >= step.Target;

                case TutorialGoal.ReachFlowState:
                    return reachedFlowState;

                case TutorialGoal.Finish:
                    return false;

                default:
                    return advanceRequested;
            }
        }

        private float StepProgress()
        {
            TutorialStepDefinition step = Current;
            float target = Mathf.Max(0.0001f, step.Target);

            switch (step.Goal)
            {
                case TutorialGoal.RunBothWays:
                    float held = Mathf.Min(leftHeldSeconds, target) + Mathf.Min(rightHeldSeconds, target);
                    return held / (target * 2f);

                case TutorialGoal.Jump:
                    return jumps / target;

                case TutorialGoal.LandHits:
                    return hitsLanded / target;

                case TutorialGoal.AnswerCorrectly:
                    return correctAnswers / target;

                case TutorialGoal.AnswerInTheGreen:
                    return fastAnswers / target;

                case TutorialGoal.ReachFlowState:
                    return ResolveFlowNormalised();

                default:
                    return 0f;
            }
        }

        private static bool HasProgressBar(TutorialGoal goal)
        {
            switch (goal)
            {
                case TutorialGoal.Continue:
                case TutorialGoal.SwingWithoutPoints:
                case TutorialGoal.Finish:
                    return false;

                default:
                    return true;
            }
        }

        private float ResolveFlowNormalised()
        {
            if (resources == null)
            {
                return 0f;
            }

            if (resources.IsFlowActive)
            {
                return 1f;
            }

            return resources.FlowNormalised;
        }

        private void ResetGoalCounters()
        {
            leftHeldSeconds = 0f;
            rightHeldSeconds = 0f;
            jumps = 0;
            hitsLanded = 0;
            swingRefused = false;
            correctAnswers = 0;
            fastAnswers = 0;
            reachedFlowState = false;
            topUpSeconds = 0f;
        }

        private void TrackMovement()
        {
            if (input == null)
            {
                return;
            }

            if (input.MoveX < -0.5f)
            {
                leftHeldSeconds += Time.deltaTime;
            }

            if (input.MoveX > 0.5f)
            {
                rightHeldSeconds += Time.deltaTime;
            }
        }

        /// <summary>
        /// Counts jumps by watching the ground leave under a rising fighter,
        /// rather than by reading the jump button. The input reader latches its
        /// press for the controller to consume, and reading it here would take
        /// the jump away from the thing the player is trying to learn.
        /// </summary>
        private void TrackJumps()
        {
            if (playerController == null)
            {
                return;
            }

            bool grounded = playerController.IsGrounded;

            if (wasGrounded && !grounded && playerController.Velocity.y > 0.1f)
            {
                jumps++;
            }

            wasGrounded = grounded;
        }

        /// <summary>
        /// Enter presses the card's main button. Space is not offered as a
        /// shortcut for the obvious reason: it is the punch.
        /// </summary>
        private void TrackEnterKey()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                HandlePrimaryPressed();
            }
        }

        /// <summary>
        /// Keeps Action Points stocked through the steps that come before the
        /// tutorial makes a lesson out of running dry.
        ///
        /// Scarcity is step five's job and nothing else's. Before that, the
        /// player is being taught which key punches, and a punch that does
        /// nothing because an invisible currency is empty teaches them the wrong
        /// thing about the key. It also has to survive the player messing about
        /// with Space during the welcome and movement steps -- which they will,
        /// long before anything invites them to.
        ///
        /// Refilled to a floor rather than instantly to full, and after a short
        /// beat rather than the same frame, so the pip still visibly drops when
        /// a punch is thrown. The cost stays legible; it just stops being a
        /// wall. The beat is far shorter than a swing, so the player never
        /// actually waits on it.
        /// </summary>
        private void TopUpPointsIfNeeded()
        {
            if (!Current.KeepsPointsTopped || resources == null)
            {
                return;
            }

            if (resources.ActionPoints >= PracticeActionPoints)
            {
                topUpSeconds = 0f;
                return;
            }

            topUpSeconds += Time.deltaTime;
            if (topUpSeconds < TopUpDelaySeconds)
            {
                return;
            }

            topUpSeconds = 0f;
            resources.AddActionPoints(1);
        }

        /// <summary>
        /// Spends whatever is left, so the step about having none actually has
        /// none. Going through the same payment call the attack uses keeps the
        /// HUD and the resources in step; there is no back door that sets the
        /// number directly.
        /// </summary>
        private void EmptyActionPoints()
        {
            if (resources == null)
            {
                return;
            }

            while (resources.TryPayForAttack())
            {
                // Nothing: the call itself is the spend.
            }
        }

        private void HideQuiz()
        {
            quizRevealed = false;

            if (quizFlow != null)
            {
                // Disabled rather than removed: a component's Start runs the
                // first time it is enabled, so this is also what holds the first
                // question back.
                quizFlow.enabled = false;
            }

            if (quizPanel != null)
            {
                quizPanel.SetActive(false);
            }

            if (quizPlaceholder != null)
            {
                quizPlaceholder.SetActive(true);
            }
        }

        private void RevealQuiz()
        {
            if (quizRevealed)
            {
                return;
            }

            quizRevealed = true;

            if (quizPlaceholder != null)
            {
                quizPlaceholder.SetActive(false);
            }

            if (quizPanel != null)
            {
                quizPanel.SetActive(true);
            }

            if (layout != null)
            {
                // The panel was inactive when the layout last ran, so its scale
                // into the quiz half is applied again now that it is on screen.
                layout.Apply();
            }

            if (quizFlow != null)
            {
                quizFlow.enabled = true;
            }
        }

        private void HandleHitLanded(AttackDefinition definition, Vector2 point)
        {
            hitsLanded++;
        }

        private void HandleAttackRefused()
        {
            swingRefused = true;
        }

        private void HandleFlowStateEntered()
        {
            reachedFlowState = true;
        }

        private void HandleQuestionResolved(QuizResult result, float speed)
        {
            if (result != QuizResult.Correct)
            {
                return;
            }

            correctAnswers++;

            if (speed > FastZoneThreshold())
            {
                fastAnswers++;
            }
        }

        /// <summary>
        /// Read off the view, so the tutorial agrees with the bar the player is
        /// watching and with what the reward bridge actually pays for.
        /// </summary>
        private float FastZoneThreshold()
        {
            if (quizView != null)
            {
                return quizView.FastZoneNormalized;
            }

            return FallbackFastZone;
        }

        private void HandlePrimaryPressed()
        {
            ClearSelection();

            if (Current.Goal == TutorialGoal.Finish)
            {
                SceneRouter.Go(fightSceneName);
                return;
            }

            advanceRequested = true;
        }

        private void HandleSecondaryPressed()
        {
            ClearSelection();
            SceneRouter.Go(menuSceneName);
        }

        private void HandleSkipPressed()
        {
            ClearSelection();
            BeginStep(steps.Count - 1);
        }

        /// <summary>
        /// Drops keyboard focus after a click.
        ///
        /// Not housekeeping: clicking a button leaves it selected, and the UI
        /// module's submit action includes Space. A selected button would
        /// therefore be pressed again by the punch key, which is the one
        /// collision this screen cannot afford.
        /// </summary>
        private static void ClearSelection()
        {
            if (EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
