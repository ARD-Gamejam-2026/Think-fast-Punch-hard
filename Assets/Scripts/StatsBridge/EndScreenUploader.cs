using System.Threading.Tasks;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Uploads the finished round's record from the end screen. Reads the static
    /// <see cref="MatchStats"/> (which survived the scene load) and the menu name,
    /// then sends it to Firebase, or to an in-memory store when no URL is set.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EndScreenUploader : MonoBehaviour
    {
        [Tooltip("Realtime Database URL, e.g. https://<project>-default-rtdb.firebaseio.com. Empty uses an in-memory store.")]
        [SerializeField] private string databaseUrl = string.Empty;

        private void Start()
        {
            if (!MatchStats.HasFinished)
            {
                return;
            }

            MatchRecord record = MatchStats.Snapshot();
            record.playerName = PlayerName.Value;
            IHighscoreBackend backend = CreateBackend();
            UploadAndForget(backend, record);
        }

        private IHighscoreBackend CreateBackend()
        {
            if (string.IsNullOrWhiteSpace(databaseUrl))
            {
                Debug.LogWarning("EndScreenUploader has no database URL, so the highscore is only kept in memory.", this);
                return new MemoryBackend();
            }

            return new RestFirebaseBackend(databaseUrl);
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
