using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThinkFast.UI
{
    /// <summary>
    /// Shows or hides a panel on click -- the overlay half of the menu, without a
    /// scene change.
    ///
    /// It also moves keyboard focus to the panel's own first button when opening,
    /// and back to itself when closing. Without that, opening a dialog with the
    /// keyboard leaves the focus ring sitting behind it on a button the player can
    /// no longer see, and the arrow keys go somewhere they should not.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class TogglePanelButton : MonoBehaviour
    {
        [Tooltip("The panel this button opens or closes.")]
        [SerializeField] private GameObject panel;

        [Tooltip("Ticked, this button opens the panel; unticked, it closes it. A close button inside the panel points back at the same panel with this off.")]
        [SerializeField] private bool opens = true;

        [Tooltip("What to focus once the panel is shown, or what to return focus to once it is hidden. Optional.")]
        [SerializeField] private GameObject focusAfter;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Toggle);
        }

        private void Toggle()
        {
            if (panel == null)
            {
                Debug.LogError("TogglePanelButton has no panel to show or hide.", this);
                return;
            }

            PlaySound();
            panel.SetActive(opens);

            if (focusAfter != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(focusAfter);
            }
        }

        private void PlaySound()
        {
            if (MenuSounds.Instance == null)
            {
                return;
            }

            if (opens)
            {
                MenuSounds.Instance.PlayPress();
                return;
            }

            MenuSounds.Instance.PlayBack();
        }
    }
}
