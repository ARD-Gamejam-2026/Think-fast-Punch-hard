using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Tutorial
{
    /// <summary>
    /// The card the tutorial talks through: a step counter, a heading, the
    /// teaching, the current objective and a progress bar, plus up to two
    /// buttons along the bottom.
    ///
    /// It knows nothing about the tutorial itself. Every string is handed to it
    /// by <see cref="TutorialDirector"/> and every press goes back out as an
    /// event, so the script the player reads lives in one place and the layout
    /// in another.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialCoach : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text counterLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text objectiveLabel;

        [Tooltip("The objective chip. Hidden entirely on a step with nothing to ask for, which is what gives the last card room for two buttons.")]
        [SerializeField] private GameObject objectiveRoot;

        [Header("Progress")]
        [Tooltip("The fill inside the objective chip. Hidden on steps that have nothing to count, leaving the chip a plain caption.")]
        [SerializeField] private GameObject progressRoot;

        [Tooltip("Fill of the progress bar. Its width is driven, so it must be anchored to the left of its track.")]
        [SerializeField] private RectTransform progressFill;

        [Header("Buttons")]
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TMP_Text secondaryLabel;

        [Tooltip("Always available, top right of the card. Leaves the tutorial.")]
        [SerializeField] private Button skipButton;

        /// <summary>The main button on the card was pressed.</summary>
        public event Action PrimaryPressed;

        /// <summary>The second button on the card was pressed. Only shown on the last step.</summary>
        public event Action SecondaryPressed;

        /// <summary>The corner button was pressed: leave the tutorial entirely.</summary>
        public event Action SkipPressed;

        private void Awake()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.AddListener(() => PrimaryPressed?.Invoke());
            }

            if (secondaryButton != null)
            {
                secondaryButton.onClick.AddListener(() => SecondaryPressed?.Invoke());
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(() => SkipPressed?.Invoke());
            }
        }

        /// <summary>Writes one step onto the card.</summary>
        public void ShowStep(string counter, string title, string body, string objective)
        {
            SetText(counterLabel, counter);
            SetText(titleLabel, title);
            SetText(bodyLabel, body);
            SetText(objectiveLabel, objective);

            // An empty objective takes the chip away rather than leaving an
            // empty pill behind, which is what clears the room the final card
            // needs for its second button.
            if (objectiveRoot != null)
            {
                objectiveRoot.SetActive(!string.IsNullOrEmpty(objective));
            }
        }

        /// <summary>
        /// Shows how far into the current goal the player is, as 0..1. Hidden
        /// entirely on steps whose goal is not a count.
        /// </summary>
        public void ShowProgress(bool visible, float normalised)
        {
            if (progressRoot != null)
            {
                progressRoot.SetActive(visible);
            }

            if (progressFill == null)
            {
                return;
            }

            // The fill stretches with its track and is trimmed from the right, so
            // the bar keeps working at any card width.
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(normalised), 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
        }

        /// <summary>Sets the main button's caption, or hides it.</summary>
        public void ShowPrimary(bool visible, string label)
        {
            Show(primaryButton, primaryLabel, visible, label);
        }

        /// <summary>Sets the second button's caption, or hides it.</summary>
        public void ShowSecondary(bool visible, string label)
        {
            Show(secondaryButton, secondaryLabel, visible, label);
        }

        private static void Show(Button button, TMP_Text label, bool visible, string caption)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }

            SetText(label, caption);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.text = value;
        }
    }
}
