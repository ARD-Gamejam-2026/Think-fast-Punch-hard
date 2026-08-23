using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Signs in anonymously through the Firebase Auth REST API and returns a
    /// short-lived ID token, so Realtime Database requests can be authenticated
    /// with no player sign-in and no Firebase SDK — only the project's public
    /// Web API key, which ships with the game.
    /// </summary>
    public sealed class FirebaseAnonymousAuth
    {
        private const string SignUpEndpoint =
            "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";

        /// <summary>Body that asks the sign-up call for a returnable secure token.</summary>
        internal const string SignInRequestBody = "{\"returnSecureToken\":true}";

        private readonly string webApiKey;

        /// <summary>Creates an anonymous auth client for the given Web API key.</summary>
        public FirebaseAnonymousAuth(string webApiKey)
        {
            this.webApiKey = webApiKey;
        }

        /// <summary>
        /// Signs in anonymously and returns the ID token, or null when the
        /// sign-in request fails or carries no token.
        /// </summary>
        public async Task<string> SignInAnonymouslyAsync()
        {
            string url = BuildSignUpUrl(webApiKey);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(SignInRequestBody);
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                await WebRequests.SendAsync(request);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Anonymous sign-in failed: " + request.error);
                    return null;
                }

                return ParseIdToken(request.downloadHandler.text);
            }
        }

        /// <summary>Builds the anonymous sign-up URL for the given Web API key.</summary>
        internal static string BuildSignUpUrl(string webApiKey)
        {
            return SignUpEndpoint + webApiKey;
        }

        /// <summary>Reads the ID token out of a sign-in response, or null.</summary>
        internal static string ParseIdToken(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            SignInResponse response = JsonUtility.FromJson<SignInResponse>(json);
            if (response == null || string.IsNullOrEmpty(response.idToken))
            {
                return null;
            }

            return response.idToken;
        }

        [System.Serializable]
        private sealed class SignInResponse
        {
            public string idToken;
            public string localId;
        }
    }
}
