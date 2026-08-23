using UnityEngine;

namespace ThinkFast.Economy
{
    /// <summary>
    /// Shows or hides a child object while the player is in Flow state.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FighterResources))]
    public sealed class FlowStateChild : MonoBehaviour
    {
        /// <summary>Turned on while Flow state is active, off otherwise.</summary>
        public GameObject flowVisual;

        private FighterResources resources;

        private void Awake()
        {
            resources = GetComponent<FighterResources>();
            SetFlowVisualActive(false);
        }

        private void OnEnable()
        {
            resources.FlowStateEntered += HandleFlowStateEntered;
            resources.FlowStateExited += HandleFlowStateExited;
            SetFlowVisualActive(resources.IsFlowActive);
        }

        private void OnDisable()
        {
            resources.FlowStateEntered -= HandleFlowStateEntered;
            resources.FlowStateExited -= HandleFlowStateExited;
            SetFlowVisualActive(false);
        }

        private void HandleFlowStateEntered()
        {
            SetFlowVisualActive(true);
        }

        private void HandleFlowStateExited()
        {
            SetFlowVisualActive(false);
        }

        private void SetFlowVisualActive(bool active)
        {
            if (flowVisual != null)
            {
                flowVisual.SetActive(active);
            }
        }
    }
}
