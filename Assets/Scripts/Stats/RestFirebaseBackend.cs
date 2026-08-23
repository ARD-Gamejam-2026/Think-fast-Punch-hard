using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Stores records in a Firebase Realtime Database over its REST API, so no
    /// Firebase Unity SDK package or google-services file is needed — only the
    /// database URL.
    /// </summary>
    public sealed class RestFirebaseBackend : IHighscoreBackend
    {
        private readonly string databaseUrl;
        private readonly FirebaseAnonymousAuth auth;

        // Anonymous ID tokens live ~1 hour. Caching one per backend instance
        // means a polling reader (the leaderboard) reuses it instead of creating
        // a fresh anonymous account on every request. Cleared on any failed
        // request so an expired token re-authenticates on the next call.
        private string cachedIdToken;

        /// <summary>
        /// Creates a backend that stores records at the given Realtime Database
        /// URL, authenticating each request with an anonymous ID token obtained
        /// from the given Web API key.
        /// </summary>
        public RestFirebaseBackend(string databaseUrl, string webApiKey)
        {
            this.databaseUrl = databaseUrl;
            this.auth = new FirebaseAnonymousAuth(webApiKey);
        }

        // Returns a cached ID token, signing in anonymously only when none is held.
        private async Task<string> GetTokenAsync()
        {
            if (string.IsNullOrEmpty(cachedIdToken))
            {
                cachedIdToken = await auth.SignInAnonymouslyAsync();
            }

            return cachedIdToken;
        }

        /// <summary>Posts the record under the highscores collection (push id).</summary>
        public async Task UploadAsync(MatchRecord record)
        {
            string idToken = await GetTokenAsync();
            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogWarning("Highscore upload skipped: anonymous sign-in returned no token.");
                return;
            }

            string url = WithAuth(BuildCollectionUrl(databaseUrl), idToken);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(SerializeRecord(record));
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                await WebRequests.SendAsync(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    cachedIdToken = null;
                    Debug.LogWarning("Highscore upload failed: " + request.error);
                }
            }
        }

        /// <summary>Gets the collection and returns the fastest records first.</summary>
        public async Task<IReadOnlyList<MatchRecord>> FetchTopAsync(int count)
        {
            string idToken = await GetTokenAsync();
            if (string.IsNullOrEmpty(idToken))
            {
                return new List<MatchRecord>();
            }

            string url = WithAuth(BuildCollectionUrl(databaseUrl), idToken);
            using (var request = UnityWebRequest.Get(url))
            {
                await WebRequests.SendAsync(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    cachedIdToken = null;
                    return new List<MatchRecord>();
                }

                List<MatchRecord> all = ParseCollection(request.downloadHandler.text);
                all.Sort((a, b) => a.DurationMillis().CompareTo(b.DurationMillis()));
                if (all.Count > count)
                {
                    all.RemoveRange(count, all.Count - count);
                }

                return all;
            }
        }

        /// <summary>Builds the highscores collection URL with exactly one slash.</summary>
        internal static string BuildCollectionUrl(string databaseUrl)
        {
            string trimmed = databaseUrl.TrimEnd('/');
            return trimmed + "/highscores.json";
        }

        /// <summary>Appends the ID token to the URL as the auth query parameter.</summary>
        internal static string WithAuth(string url, string idToken)
        {
            return url + "?auth=" + idToken;
        }

        /// <summary>Serializes the record to the JSON body Firebase stores.</summary>
        internal static string SerializeRecord(MatchRecord record)
        {
            return JsonUtility.ToJson(record);
        }

        /// <summary>
        /// Parses a Realtime Database collection response — an object keyed by
        /// push ids — into records. Returns empty for a null or blank response.
        /// </summary>
        internal static List<MatchRecord> ParseCollection(string json)
        {
            var records = new List<MatchRecord>();
            if (string.IsNullOrWhiteSpace(json) || json == "null")
            {
                return records;
            }

            foreach (string valueObject in FirebaseCollectionReader.SplitValueObjects(json))
            {
                records.Add(JsonUtility.FromJson<MatchRecord>(valueObject));
            }

            return records;
        }
    }
}
