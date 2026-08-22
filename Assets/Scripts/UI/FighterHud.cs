using ThinkFast.Combat;
using ThinkFast.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.UI
{
    /// <summary>
    /// The fighter's real in-game HUD: health, Action Points and Flow, as uGUI
    /// objects in the scene rather than IMGUI debug text. Restyle it in the
    /// editor -- nothing here hard-codes an appearance beyond the colours.
    ///
    /// Bars and pips only, no text. TextMeshPro's essential resources are not in
    /// the project yet (they arrive with the menu branch), and importing a second
    /// copy would collide with it. Numbers can be added on top later; the shapes
    /// are the part you actually read mid-fight anyway.
    ///
    /// Polls rather than subscribing: values change most frames regardless, and
    /// polling cannot get out of sync if a target is swapped at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FighterHud : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Left empty, the HUD finds the player's FighterResources on Start and reads the Health next to it.")]
        [SerializeField] private Health health;

        [SerializeField] private FighterResources resources;

        [Header("Health")]
        [SerializeField] private RectTransform healthFill;

        [SerializeField] private Image healthFillImage;
        [SerializeField] private Color healthColour = new Color(0.30f, 0.85f, 0.40f);
        [SerializeField] private Color healthLowColour = new Color(0.90f, 0.25f, 0.25f);

        [Tooltip("Health fraction below which the bar turns to the low colour.")]
        [SerializeField, Range(0f, 1f)] private float healthLowThreshold = 0.3f;

        [Header("Flow")]
        [SerializeField] private RectTransform flowFill;

        [SerializeField] private Image flowFillImage;
        [SerializeField] private Color flowColour = new Color(0.45f, 0.60f, 1f);
        [SerializeField] private Color flowActiveColour = new Color(1f, 0.80f, 0.15f);

        [Header("Action Points")]
        [SerializeField] private Image[] actionPointPips;

        [SerializeField] private Color pipFilledColour = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] private Color pipEmptyColour = new Color(1f, 1f, 1f, 0.18f);

        [Header("Flow state emphasis")]
        [Tooltip("How much the flow bar pulses while Flow state is active.")]
        [SerializeField] private float flowPulseAmount = 0.12f;

        [SerializeField] private float flowPulseSpeed = 8f;

        private RectTransform flowFillParent;

        private void Start()
        {
            if (resources == null)
            {
                resources = FindAnyObjectByType<FighterResources>();
            }

            if (health == null)
            {
                // Found via the economy, not directly. There are two Health
                // components in a fight now, and FindAnyObjectByType picks an
                // arbitrary one -- which would leave the player watching the
                // opponent's health bar. Only the player has FighterResources,
                // so it is the reliable way to identify them.
                health = resources != null
                    ? resources.GetComponent<Health>()
                    : FindAnyObjectByType<Health>();
            }

            if (flowFill != null)
            {
                flowFillParent = flowFill.parent as RectTransform;
            }
        }

        private void LateUpdate()
        {
            UpdateHealth();
            UpdateFlow();
            UpdateActionPoints();
        }

        private void UpdateHealth()
        {
            if (health == null || healthFill == null)
            {
                return;
            }

            SetFill(healthFill, health.Normalised);

            if (healthFillImage != null)
            {
                healthFillImage.color = health.Normalised <= healthLowThreshold
                    ? healthLowColour
                    : healthColour;
            }
        }

        private void UpdateFlow()
        {
            if (resources == null || flowFill == null)
            {
                return;
            }

            SetFill(flowFill, resources.FlowNormalised);

            if (flowFillImage != null)
            {
                flowFillImage.color = resources.IsFlowActive ? flowActiveColour : flowColour;
            }

            // Pulse the whole bar during Flow state so it reads as "active" even
            // in peripheral vision, while the eye is on the fight.
            if (flowFillParent != null)
            {
                float scale = resources.IsFlowActive
                    ? 1f + ((Mathf.Sin(Time.time * flowPulseSpeed) * 0.5f + 0.5f) * flowPulseAmount)
                    : 1f;

                flowFillParent.localScale = new Vector3(1f, scale, 1f);
            }
        }

        private void UpdateActionPoints()
        {
            if (resources == null || actionPointPips == null)
            {
                return;
            }

            for (int i = 0; i < actionPointPips.Length; i++)
            {
                Image pip = actionPointPips[i];
                if (pip == null)
                {
                    continue;
                }

                pip.color = i < resources.ActionPoints ? pipFilledColour : pipEmptyColour;
            }
        }

        /// <summary>
        /// Drives the fill by anchor rather than <see cref="Image.fillAmount"/>,
        /// because a filled Image needs a sprite assigned and these bars are
        /// deliberately sprite-less white quads.
        /// </summary>
        private static void SetFill(RectTransform fill, float normalised)
        {
            Vector2 anchorMax = fill.anchorMax;
            anchorMax.x = Mathf.Clamp01(normalised);
            fill.anchorMax = anchorMax;

            // Offsets are deliberately left alone. A RectTransform is anchors
            // PLUS offsets, so moving the anchor is enough -- and the offsets are
            // carrying the bar's inset border, which zeroing here would erase on
            // the first frame.
        }
    }
}
