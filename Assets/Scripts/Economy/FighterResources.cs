using System;
using UnityEngine;

namespace ThinkFast.Economy
{
    /// <summary>
    /// The fighter's two resources.
    ///
    /// Action Points gate whether you can attack at all: one per swing, capped so
    /// they cannot be hoarded by camping the puzzle half.
    ///
    /// Flow is a pressure gauge rather than a bank. It drains constantly, so
    /// standing still loses it; only a steady stream of fast solves pushes it to
    /// full. Reaching full flips the fighter into Flow state, where hits multiply
    /// but the meter empties much faster, cannot be topped up, and is burned
    /// further by every swing -- so it is strictly a window, never a plateau, and
    /// using it is what ends it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FighterResources : MonoBehaviour, IRiddleRewardSink
    {
        [Header("Action Points")]
        [Tooltip("Upper bound on stored AP. This is what stops the player farming the puzzle half and banking a huge reserve.")]
        [SerializeField] private int maxActionPoints = 5;

        [SerializeField] private int startingActionPoints = 2;

        [Header("Flow")]
        [SerializeField] private float maxFlow = 100f;

        [Tooltip("Constant bleed while not in Flow state. Flow is a gauge you have to keep feeding, not a bank you fill up.")]
        [SerializeField] private float flowDrainPerSecond = 6f;

        [Tooltip("Bleed while Flow state is active. Much faster, so the payoff is a burst window with a hard clock on it.")]
        [SerializeField] private float flowStateDrainPerSecond = 20f;

        [Tooltip("Flow burned by each swing during Flow state, on top of the constant bleed. This is what makes the burst something you SPEND rather than something you sit inside: swinging is what ends it, so how long the window lasts is a decision, not a timer.")]
        [SerializeField] private float flowStateCostPerAttack = 10f;

        [Header("Flow state payoff")]
        [Tooltip("Damage multiplier while Flow state is active.")]
        [SerializeField] private float flowDamageMultiplier = 2f;

        [Tooltip("Knockback multiplier while Flow state is active. Separate from damage so the hit can be made to LOOK bigger independently of how hard it actually hits.")]
        [SerializeField] private float flowKnockbackMultiplier = 1.6f;

        public int ActionPoints { get; private set; }

        public int MaxActionPoints => maxActionPoints;

        public float Flow { get; private set; }

        public float MaxFlow => maxFlow;

        /// <summary>Flow as 0..1, for meters and shaders.</summary>
        public float FlowNormalised => maxFlow > 0f ? Mathf.Clamp01(Flow / maxFlow) : 0f;

        public bool IsFlowActive { get; private set; }

        public float DamageMultiplier => IsFlowActive ? flowDamageMultiplier : 1f;

        public float KnockbackMultiplier => IsFlowActive ? flowKnockbackMultiplier : 1f;

        public event Action<int> ActionPointsChanged;
        public event Action<float> FlowChanged;
        public event Action FlowStateEntered;
        public event Action FlowStateExited;

        /// <summary>Raised when an attack was wanted but there was no AP to pay for it.</summary>
        public event Action ActionPointsExhausted;

        private void Awake()
        {
            ActionPoints = Mathf.Clamp(startingActionPoints, 0, maxActionPoints);
            Flow = 0f;
            IsFlowActive = false;
        }

        private void OnEnable()
        {
            RiddleRewards.Register(this);
        }

        private void OnDisable()
        {
            RiddleRewards.Unregister(this);
        }

        private void Update()
        {
            float drainRate = IsFlowActive ? flowStateDrainPerSecond : flowDrainPerSecond;
            SetFlow(Flow - (drainRate * Time.deltaTime));
        }

        /// <summary>
        /// Called by the puzzle half, via <see cref="RiddleRewards"/>.
        /// </summary>
        public void GrantSolve(float flowReward)
        {
            AddActionPoints(1);
            AddFlow(flowReward);
        }

        public void AddActionPoints(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int updated = Mathf.Clamp(ActionPoints + amount, 0, maxActionPoints);
            if (updated == ActionPoints)
            {
                return;
            }

            ActionPoints = updated;
            ActionPointsChanged?.Invoke(ActionPoints);
        }

        /// <summary>
        /// Adds Flow, unless Flow state is already running.
        ///
        /// Refusing Flow during the state is the rule that gives the whole
        /// economy its shape: the burst cannot be extended by solving faster, so
        /// once it starts the only thing left to do is convert it into damage.
        /// AP still accrues normally, so the puzzle half never becomes pointless.
        /// </summary>
        public void AddFlow(float amount)
        {
            if (IsFlowActive || amount <= 0f)
            {
                return;
            }

            SetFlow(Flow + amount);
        }

        /// <summary>
        /// Pays the full cost of one swing: always an Action Point, plus a bite
        /// out of the Flow gauge while Flow state is running. Returns false when
        /// there is no AP, in which case nothing is charged at all.
        ///
        /// Both costs live behind one call on purpose. Two separate methods would
        /// let a caller pay one and forget the other, and the Flow burn is not
        /// optional -- it is the rule that stops Flow state being a free five
        /// seconds of double damage.
        /// </summary>
        public bool TryPayForAttack()
        {
            if (ActionPoints <= 0)
            {
                ActionPointsExhausted?.Invoke();
                return false;
            }

            ActionPoints--;
            ActionPointsChanged?.Invoke(ActionPoints);

            // Charged at swing start, alongside the AP. The multiplier is read
            // again when the hit actually lands, so a swing that empties the
            // gauge lands at ×1 -- the Flow really did run out mid-punch.
            if (IsFlowActive && flowStateCostPerAttack > 0f)
            {
                SetFlow(Flow - flowStateCostPerAttack);
            }

            return true;
        }

        /// <summary>Debug helper: wipes both resources back to their starting values.</summary>
        public void ResetResources()
        {
            if (IsFlowActive)
            {
                IsFlowActive = false;
                FlowStateExited?.Invoke();
            }

            Flow = 0f;
            FlowChanged?.Invoke(Flow);

            ActionPoints = Mathf.Clamp(startingActionPoints, 0, maxActionPoints);
            ActionPointsChanged?.Invoke(ActionPoints);
        }

        private void SetFlow(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxFlow);
            bool changed = !Mathf.Approximately(clamped, Flow);

            Flow = clamped;

            if (changed)
            {
                FlowChanged?.Invoke(Flow);
            }

            // Transitions are checked outside the change guard: the frame that
            // settles exactly on a boundary still has to flip the state.
            if (!IsFlowActive && Flow >= maxFlow)
            {
                IsFlowActive = true;
                FlowStateEntered?.Invoke();
            }
            else if (IsFlowActive && Flow <= 0f)
            {
                IsFlowActive = false;
                FlowStateExited?.Invoke();
            }
        }
    }
}
