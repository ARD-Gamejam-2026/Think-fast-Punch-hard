using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Uploads the finished round's record from the end screen. Reads the static
    /// <see cref="MatchStats"/> (which survived the scene load) and the player
    /// name, then sends it to Firebase, or to an in-memory store when no URL is
    /// set. When no name was set on the menu, it first prompts for one and
    /// uploads on confirm.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EndScreenUploader : MonoBehaviour
    {
        [Tooltip("Realtime Database URL, e.g. https://<project>-default-rtdb.firebaseio.com. Empty uses an in-memory store.")]
        [SerializeField] private string databaseUrl = "https://think-fast-punch-fast-default-rtdb.europe-west1.firebasedatabase.app";

        [Tooltip("Firebase Web API key (Project settings > General). Public/embeddable; ships with the game. Used to sign in anonymously so uploads are authenticated. Empty uses an in-memory store.")]
        [SerializeField] private string webApiKey = "AIzaSyDHZ-mRPzoeJyC87NnmP8UfV1IkxSaTEY4";

        [Header("Name prompt (shown only when no name was set on the menu)")]
        [Tooltip("Panel holding the name field and save button. Left hidden until needed.")]
        [SerializeField] private GameObject namePrompt;

        [Tooltip("The field the player types their name into on the end screen.")]
        [SerializeField] private TMP_InputField nameField;

        [Tooltip("The button that confirms the name and uploads the score.")]
        [SerializeField] private Button saveButton;

        private MatchRecord pendingRecord;
        private bool uploaded;

        private void Start()
        {
            if (!MatchStats.HasFinished)
            {
                return;
            }

            pendingRecord = MatchStats.Snapshot();

            if (PlayerName.IsSet)
            {
                HidePrompt();
                Submit(PlayerName.Value);
                return;
            }

            ShowPrompt();
        }

        private void OnDisable()
        {
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(HandleSave);
            }
        }

        // Reveals the name prompt and waits for the save button. With no prompt
        // wired it uploads under the default name so the score is not lost.
        private void ShowPrompt()
        {
            if (namePrompt == null || nameField == null || saveButton == null)
            {
                Debug.LogWarning("EndScreenUploader has no name prompt wired; uploading under the default name.", this);
                Submit(PlayerName.Value);
                return;
            }

            namePrompt.SetActive(true);
            nameField.text = string.Empty;
            saveButton.onClick.AddListener(HandleSave);
        }

        private void HidePrompt()
        {
            if (namePrompt != null)
            {
                namePrompt.SetActive(false);
            }
        }

        private void HandleSave()
        {
            PlayerName.Set(nameField.text);
            HidePrompt();
            Submit(PlayerName.Value);
        }

        // Stamps the name onto the record and uploads it once.
        private void Submit(string playerName)
        {
            if (uploaded)
            {
                return;
            }

            uploaded = true;
            pendingRecord.playerName = playerName;
            IHighscoreBackend backend = CreateBackend();
            UploadAndForget(backend, pendingRecord);
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
