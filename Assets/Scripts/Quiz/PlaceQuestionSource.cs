using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Prefetches fully built place-guessing questions (landmark photo from
    /// Wikipedia + four place names) into a small ready-queue so gameplay
    /// never waits on the network. Fetch failures log a warning, back off,
    /// and continue; offline simply means an empty queue.
    /// </summary>
    public class PlaceQuestionSource : MonoBehaviour
    {
        private const string SummaryUrl = "https://en.wikipedia.org/api/rest_v1/page/summary/";
        private const int RequestTimeoutSeconds = 10;

        [SerializeField] private LandmarkList landmarks;
        [SerializeField, Min(1)] private int queueTargetSize = 2;
        [SerializeField, Min(1f)] private float timeLimitSeconds = 5f;
        [SerializeField, Min(0f)] private float retryDelaySeconds = 5f;

        private readonly Queue<QuizQuestion> ready = new Queue<QuizQuestion>();
        private LandmarkDeck deck;
        private System.Random random;

        /// <summary>True when a prefetched question is ready to show.</summary>
        public bool HasQuestion => ready.Count > 0;

        /// <summary>
        /// Removes and returns the next prefetched question. Only call when
        /// HasQuestion is true; the caller owns the runtime question and its
        /// sprite/texture and must destroy them once no longer shown.
        /// </summary>
        public QuizQuestion Dequeue()
        {
            return ready.Dequeue();
        }

        private void Start()
        {
            if (landmarks == null || landmarks.entries == null
                || landmarks.entries.Length < QuizQuestion.AnswerCount)
            {
                Debug.LogError(
                    $"PlaceQuestionSource needs a LandmarkList with at least {QuizQuestion.AnswerCount} entries",
                    this);
                enabled = false;
                return;
            }

            random = new System.Random();
            deck = new LandmarkDeck(landmarks.entries.Length, random);
            StartCoroutine(FillQueue());
        }

        private IEnumerator FillQueue()
        {
            while (true)
            {
                if (ready.Count >= queueTargetSize)
                {
                    yield return null;
                    continue;
                }

                var entry = landmarks.entries[deck.Next()];

                WikipediaSummary summary = null;
                // Titles may contain non-ASCII or reserved characters
                // (Sagrada_Família, St._Peter's_Basilica) — escape them.
                using (var request = UnityWebRequest.Get(
                    SummaryUrl + UnityWebRequest.EscapeURL(entry.wikipediaTitle)))
                {
                    yield return SendWithTimeout(request);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // JsonUtility.FromJson throws on malformed JSON; a
                        // single bad response must not kill this coroutine.
                        try
                        {
                            summary = JsonUtility.FromJson<WikipediaSummary>(
                                request.downloadHandler.text);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning(
                                $"PlaceQuestionSource: summary parse failed for {entry.wikipediaTitle}: {e.Message}",
                                this);
                        }
                    }
                }

                if (summary == null)
                {
                    Debug.LogWarning($"PlaceQuestionSource: summary fetch failed for {entry.wikipediaTitle}", this);
                    yield return new WaitForSeconds(retryDelaySeconds);
                    continue;
                }

                if (string.IsNullOrEmpty(summary.thumbnail?.source))
                {
                    Debug.LogWarning($"PlaceQuestionSource: no thumbnail for {entry.wikipediaTitle}", this);
                    continue;
                }

                Texture2D texture = null;
                // Spike rule: fetch the API-returned URL verbatim; constructed
                // resize URLs return 400.
                using (var request = UnityWebRequestTexture.GetTexture(summary.thumbnail.source))
                {
                    yield return SendWithTimeout(request);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        texture = DownloadHandlerTexture.GetContent(request);
                    }
                }

                if (texture == null)
                {
                    Debug.LogWarning($"PlaceQuestionSource: image fetch failed for {entry.wikipediaTitle}", this);
                    yield return new WaitForSeconds(retryDelaySeconds);
                    continue;
                }

                // Sprite.Create/PlaceAnswerBuilder could throw; treat that
                // like any other fetch failure instead of ending the loop.
                QuizQuestion question = null;
                try
                {
                    question = BuildQuestion(entry, texture);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"PlaceQuestionSource: building question failed for {entry.wikipediaTitle}: {e.Message}",
                        this);
                }

                if (question == null)
                {
                    yield return new WaitForSeconds(retryDelaySeconds);
                    continue;
                }

                ready.Enqueue(question);
            }
        }

        /// <summary>
        /// Sends the request and waits for completion. UnityWebRequest.timeout
        /// is a no-op on WebGL (our build target), so the request is also
        /// raced against a manual clock and aborted on expiry — an aborted
        /// request reports a non-success result.
        /// </summary>
        private static IEnumerator SendWithTimeout(UnityWebRequest request)
        {
            request.timeout = RequestTimeoutSeconds;
            var operation = request.SendWebRequest();
            float start = Time.realtimeSinceStartup;
            while (!operation.isDone
                && Time.realtimeSinceStartup - start < RequestTimeoutSeconds)
            {
                yield return null;
            }

            if (!request.isDone)
            {
                request.Abort();
            }
        }

        private QuizQuestion BuildQuestion(LandmarkList.Entry entry, Texture2D texture)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            int correctEntryIndex = System.Array.IndexOf(landmarks.entries, entry);
            var answers = PlaceAnswerBuilder.Build(
                landmarks.entries, correctEntryIndex, random, out int correctIndex);

            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.name = $"Place {entry.displayName}";
            question.questionText = "Which place is this?";
            question.image = sprite;
            question.answers = answers;
            question.correctIndex = correctIndex;
            question.timeLimitSeconds = timeLimitSeconds;
            return question;
        }
    }
}
