using TMPro;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Binds a menu text field to <see cref="PlayerName"/>: saves whatever the
    /// player types for their highscore entries. The field is left empty so its
    /// placeholder shows rather than pre-selecting a stored name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerNameField : MonoBehaviour
    {
        [Tooltip("The input field the player types their name into.")]
        [SerializeField] private TMP_InputField field;

        private void OnEnable()
        {
            if (field == null)
            {
                Debug.LogError("PlayerNameField has no input field, so the name cannot be edited.", this);
                return;
            }

            // Deliberately not prefilled from PlayerName: the field shows its
            // "Your name" placeholder instead of pre-selecting a saved name.
            field.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDisable()
        {
            if (field != null)
            {
                field.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(string value)
        {
            PlayerName.Set(value);
        }
    }
}
