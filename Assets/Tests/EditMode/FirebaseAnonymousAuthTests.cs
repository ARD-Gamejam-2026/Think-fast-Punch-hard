using NUnit.Framework;
using ThinkFast.Stats;

namespace ThinkFast.Stats.Tests
{
    public class FirebaseAnonymousAuthTests
    {
        [Test]
        public void Sign_up_url_targets_the_identity_toolkit_with_the_web_api_key()
        {
            Assert.AreEqual(
                "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=ABC123",
                FirebaseAnonymousAuth.BuildSignUpUrl("ABC123"));
        }

        [Test]
        public void Sign_in_request_body_asks_for_a_secure_token()
        {
            StringAssert.Contains("\"returnSecureToken\":true", FirebaseAnonymousAuth.SignInRequestBody);
        }

        [Test]
        public void Parse_id_token_reads_the_token_from_a_sign_in_response()
        {
            string response =
                "{\"kind\":\"identitytoolkit#SignupNewUserResponse\"," +
                "\"idToken\":\"TOKEN123\",\"refreshToken\":\"r\"," +
                "\"expiresIn\":\"3600\",\"localId\":\"uid42\"}";

            Assert.AreEqual("TOKEN123", FirebaseAnonymousAuth.ParseIdToken(response));
        }

        [Test]
        public void Parse_id_token_is_null_for_empty_or_tokenless_responses()
        {
            Assert.IsNull(FirebaseAnonymousAuth.ParseIdToken(""));
            Assert.IsNull(FirebaseAnonymousAuth.ParseIdToken("{\"error\":\"bad\"}"));
        }

        [Test]
        public void Rest_backend_appends_the_id_token_as_the_auth_query_parameter()
        {
            Assert.AreEqual(
                "https://db.firebaseio.com/highscores.json?auth=TKN",
                RestFirebaseBackend.WithAuth("https://db.firebaseio.com/highscores.json", "TKN"));
        }
    }
}
