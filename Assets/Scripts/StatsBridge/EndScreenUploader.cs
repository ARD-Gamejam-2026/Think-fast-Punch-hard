using System.Collections.Generic;
using ThinkFast.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Uploads the finished round's record from the end screen. Reads the static
    /// <see cref="MatchStats"/> (which survived the scene load), shows a name
    /// field pre-filled with the current name, and uploads to Firebase (or an
    /// in-memory store when no URL is set) when the player leaves the end screen
    /// via a scene button (Fight Again / Menu) — those double as the confirm.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EndScreenUploader : MonoBehaviour
    {
        [Tooltip("Realtime Database URL, e.g. https://<project>-default-rtdb.firebaseio.com. Empty uses an in-memory store.")]
        [SerializeField] private string databaseUrl = "https://think-fast-punch-fast-default-rtdb.europe-west1.firebasedatabase.app";

        [Tooltip("Firebase Web API key (Project settings > General). Public/embeddable; ships with the game. Used to sign in anonymously so uploads are authenticated. Empty uses an in-memory store.")]
        [SerializeField] private string webApiKey = "AIzaSyDHZ-mRPzoeJyC87NnmP8UfV1IkxSaTEY4";

        [Header("Name")]
        [Tooltip("The end-screen field the player reviews or edits their name in.")]
        [SerializeField] private TMP_InputField nameField;

        private MatchRecord pendingRecord;
        private bool uploaded;
        private readonly List<Button> leaveButtons = new List<Button>();

        private void Start()
        {
            if (!MatchStats.HasFinished)
            {
                return;
            }

            pendingRecord = MatchStats.Snapshot();
            PrefillField();
            HookLeaveButtons();
        }

        private void OnDisable()
        {
            if (nameField != null)
            {
                nameField.onValueChanged.RemoveListener(PlayerName.Set);
            }

            foreach (Button button in leaveButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(Confirm);
                }
            }

            leaveButtons.Clear();
        }

        // Shows the current name in the field and keeps PlayerName in step with
        // edits, so leaving the screen uploads whatever is shown.
        private void PrefillField()
        {
            if (nameField == null)
            {
                return;
            }

            if (PlayerName.IsSet)
            {
                nameField.text = PlayerName.Value;
            }
            else
            {
                nameField.text = string.Empty;
            }

            nameField.onValueChanged.AddListener(PlayerName.Set);
        }

        // Makes the scene buttons (Fight Again / Menu) the confirm: leaving the
        // end screen uploads. With no such button it uploads right away.
        private void HookLeaveButtons()
        {
            foreach (LoadSceneButton leave in FindObjectsByType<LoadSceneButton>(FindObjectsInactive.Include))
            {
                Button button = leave.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(Confirm);
                    leaveButtons.Add(button);
                }
            }

            if (leaveButtons.Count == 0)
            {
                Debug.LogWarning("EndScreenUploader found no scene buttons to confirm on; uploading now.", this);
                Confirm();
            }
        }

        // Stamps the shown name onto the record and uploads it once.
        private void Confirm()
        {
            if (uploaded)
            {
                return;
            }

            uploaded = true;
            if (nameField != null)
            {
                PlayerName.Set(nameField.text);
            }

            pendingRecord.playerName = PlayerName.Value;
            UploadAndForget(CreateBackend(), pendingRecord);
        }

        private IHighscoreBackend CreateBackend()
        {
            if (string.IsNullOrWhiteSpace(databaseUrl) || string.IsNullOrWhiteSpace(webApiKey))
            {
                Debug.LogWarning("EndScreenUploader is missing the database URL or Web API key, so the highscore is only kept in memory.", this);
                return new MemoryBackend();
            }

            return new RestFirebaseBackend(databaseUrl, webApiKey);
        }

        private async void UploadAndForget(IHighscoreBackend backend, MatchRecord record)
        {
            try
            {
                await backend.UploadAsync(record);
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("Highscore upload failed: " + error.Message, this);
            }
        }
    }
}
