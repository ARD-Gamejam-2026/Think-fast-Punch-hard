using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Quiz
{
    /// <summary>One of the four A-D answer choices. Presentation only.</summary>
    public class AnswerButton : MonoBehaviour
    {
        /// <summary>Feedback states an answer button can display.</summary>
        public enum VisualState
        {
            Normal,
            Correct,     // green: this was the right answer and the player picked it
            Wrong,       // red: the player picked this and it was wrong
            Highlighted, // gold: reveals the right answer after Wrong/TimedOut
        }

        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text letterLabel;
        [SerializeField] private TMP_Text answerLabel;

        [SerializeField] private Color normalColor = new Color(0.10f, 0.10f, 0.28f);
        [SerializeField] private Color correctColor = new Color(0.15f, 0.55f, 0.20f);
        [SerializeField] private Color wrongColor = new Color(0.72f, 0.12f, 0.18f);
        [SerializeField] private Color highlightColor = new Color(0.75f, 0.62f, 0.12f);

        private int index;
        private Action<int> onClicked;

        /// <summary>Initializes the button with its index, letter label, and click callback.</summary>
        public void Initialize(int index, string letter, Action<int> onClicked)
        {
            this.index = index;
            this.onClicked = onClicked;
            letterLabel.text = letter;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClicked?.Invoke(this.index));
        }

        public void SetAnswerText(string text)
        {
            answerLabel.text = text;
        }

        public void SetInteractable(bool value)
        {
            button.interactable = value;
        }

        /// <summary>Applies the background color for the given feedback state.</summary>
        public void SetVisualState(VisualState state)
        {
            background.color = state switch
            {
                VisualState.Correct => correctColor,
                VisualState.Wrong => wrongColor,
                VisualState.Highlighted => highlightColor,
                _ => normalColor,
            };
        }
    }
}
