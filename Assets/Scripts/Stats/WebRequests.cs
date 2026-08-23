using System.Threading.Tasks;
using UnityEngine.Networking;

namespace ThinkFast.Stats
{
    /// <summary>
    /// Small helper that awaits a <see cref="UnityWebRequest"/>, shared by the
    /// REST backend and the anonymous auth client so the send-and-await pattern
    /// is written once.
    /// </summary>
    internal static class WebRequests
    {
        /// <summary>Sends the request and completes when the web request finishes.</summary>
        internal static Task SendAsync(UnityWebRequest request)
        {
            var completion = new TaskCompletionSource<bool>();
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => { completion.SetResult(true); };
            return completion.Task;
        }
    }
}
