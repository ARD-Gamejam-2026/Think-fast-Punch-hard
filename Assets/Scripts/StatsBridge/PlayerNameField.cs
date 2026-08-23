using TMPro;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Binds a menu text field to <see cref="PlayerName"/>: shows the stored name
    /// and saves whatever the player types for their highscore entries.
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

            field.text = PlayerName.Value;
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
