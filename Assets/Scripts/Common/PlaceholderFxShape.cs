using UnityEngine;

namespace ThinkFast.Common
{
    /// <summary>
    /// THROWAWAY. Scales its own object between two sizes and then destroys it.
    ///
    /// Self-contained rather than a coroutine on the fighter, so the cue's
    /// lifetime does not depend on the fighter still being enabled -- a swing
    /// interrupted by a knockout would otherwise leave its shape on screen
    /// forever.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaceholderFxShape : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private float lifetime;
        private float elapsed;

        public void Play(Vector3 fromScale, Vector3 toScale, float duration)
        {
            from = fromScale;
            to = toScale;
            lifetime = Mathf.Max(0.01f, duration);
            elapsed = 0f;
            transform.localScale = from;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / lifetime);
            transform.localScale = Vector3.Lerp(from, to, progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
