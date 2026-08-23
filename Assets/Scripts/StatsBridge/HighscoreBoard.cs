using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ThinkFast.Stats
{
    /// <summary>
    /// A self-updating leaderboard on the start menu. Polls the highscore backend
    /// on a timer, keeps only wins (fastest first), and fills a set of rows cloned
    /// from a template built by the menu generator. Runtime only: it never uses the
    /// editor-side <c>UiFactory</c>, so a cloned row is the only shape it needs.
    ///
    /// The rows come from the generator rather than being laid out here so a rebuild
    /// of the menu carries the whole board with it, the way every other generated
    /// piece of the menu does.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighscoreBoard : MonoBehaviour
    {
        // The named children a cloned row must carry, one per column.
        private const string RankChild = "Rank";
        private const string NameChild = "Name";
        private const string TimeChild = "Time";
        private const string QuizChild = "Quiz";
        private const string DamageChild = "Dmg";

        private const string LoadingText = "Loading…";
        private const string EmptyText = "No wins yet — be the first.";

        [Tooltip("Realtime Database URL. Empty uses an in-memory store (always empty on the menu).")]
        [SerializeField] private string databaseUrl = "https://think-fast-punch-fast-default-rtdb.europe-west1.firebasedatabase.app";

        [Tooltip("Firebase Web API key. Public/embeddable; ships with the game. Empty uses an in-memory store.")]
        [SerializeField] private string webApiKey = "AIzaSyDHZ-mRPzoeJyC87NnmP8UfV1IkxSaTEY4";

        [Tooltip("Inactive prototype row cloned once per entry. Must carry the Rank/Name/Time/Quiz/Dmg labels.")]
        [SerializeField] private RectTransform rowTemplate;

        [Tooltip("Parent the cloned rows are placed under, top down.")]
        [SerializeField] private RectTransform rowContainer;

        [Tooltip("Shown while loading and when there are no records to list.")]
        [SerializeField] private TMP_Text statusLabel;

        [Tooltip("How many rows the board has room for.")]
        [SerializeField] private int maxRows = 8;

        [Tooltip("Vertical step between rows, in reference pixels.")]
        [SerializeField] private float rowHeight = 56f;

        [Tooltip("Seconds between refreshes.")]
        [SerializeField] private float refreshSeconds = 5f;

        [Tooltip("How many records to pull before filtering to wins. Over-fetched so wins are not hidden behind faster losses.")]
        [SerializeField] private int fetchCount = 200;

        private IHighscoreBackend backend;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private IEnumerator Start()
        {
            backend = CreateBackend();
            if (statusLabel != null)
            {
                statusLabel.text = LoadingText;
                statusLabel.enabled = true;
            }

            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }

            var wait = new WaitForSeconds(Mathf.Max(1f, refreshSeconds));
            while (true)
            {
                yield return Refresh();
                yield return wait;
            }
        }

        private IEnumerator Refresh()
        {
            if (backend == null || rowTemplate == null || rowContainer == null)
            {
                yield break;
            }

            Task<IReadOnlyList<MatchRecord>> task = backend.FetchTopAsync(fetchCount);
            yield return new WaitUntil(() => task.IsCompleted);

            // A failed fetch keeps whatever is already on the board rather than
            // flashing it empty; the next poll tries again.
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("Highscore fetch failed; keeping the current board.");
                yield break;
            }

            Populate(Leaderboard.RankWins(task.Result, maxRows));
        }

        private IHighscoreBackend CreateBackend()
        {
            if (string.IsNullOrWhiteSpace(databaseUrl) || string.IsNullOrWhiteSpace(webApiKey))
            {
                Debug.LogWarning("HighscoreBoard has no database URL or Web API key, so it reads from an empty in-memory store.", this);
                return new MemoryBackend();
            }

            return new RestFirebaseBackend(databaseUrl, webApiKey);
        }

        private void Populate(IReadOnlyList<MatchRecord> wins)
        {
            foreach (GameObject row in spawnedRows)
            {
                Destroy(row);
            }

            spawnedRows.Clear();

            if (wins == null || wins.Count == 0)
            {
                if (statusLabel != null)
                {
                    statusLabel.text = EmptyText;
                    statusLabel.enabled = true;
                }

                return;
            }

            if (statusLabel != null)
            {
                statusLabel.enabled = false;
            }

            for (int i = 0; i < wins.Count; i++)
            {
                MatchRecord record = wins[i];
                RectTransform row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                row.anchoredPosition = new Vector2(row.anchoredPosition.x, -i * rowHeight);

                SetChild(row, RankChild, (i + 1).ToString());
                SetChild(row, NameChild, DisplayName(record.playerName));
                SetChild(row, TimeChild, Leaderboard.FormatDuration(record.DurationMillis()));
                SetChild(row, QuizChild, QuizText(record));
                SetChild(row, DamageChild, DamageText(record));

                spawnedRows.Add(row.gameObject);
            }
        }

        private static void SetChild(RectTransform row, string childName, string text)
        {
            Transform child = row.Find(childName);
            if (child == null)
            {
                return;
            }

            var label = child.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        // Keeps a long name from spilling into the next column.
        private static string DisplayName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "anon";
            }

            const int Max = 12;
            if (name.Length <= Max)
            {
                return name;
            }

            return name.Substring(0, Max - 1) + "…";
        }

        private static string QuizText(MatchRecord record)
        {
            int total = record.quizzesRight + record.quizzesWrong + record.quizzesTimedOut;
            return record.quizzesRight + "/" + total;
        }

        private static string DamageText(MatchRecord record)
        {
            return record.damageDealt + "/" + record.damageTaken;
        }
    }
}
