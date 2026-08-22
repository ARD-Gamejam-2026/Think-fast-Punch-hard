using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.UI
{
    /// <summary>
    /// Washes the screen in when it loads and out again before the next one.
    ///
    /// A scene load is instant and jarring, and the seam is most visible exactly
    /// where this game changes screens most often. Fading through white rather
    /// than black keeps the menus feeling light instead of turning every
    /// transition into a blackout.
    ///
    /// The sheet also swallows input while it is washing out, which is what stops
    /// a second click landing on a screen that is already leaving -- the fastest
    /// way to load a scene twice.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFade : MonoBehaviour
    {
        [Tooltip("Full-screen image the fade is painted onto. It sits above everything else on its canvas.")]
        [SerializeField] private Image sheet;

        private float alpha = 1f;
        private bool leaving;
        private string pendingScene;

        private static ScreenFade instance;

        /// <summary>
        /// The fade in the current scene, or null when a screen was built without
        /// one. Resolved lazily so buttons need no reference to it.
        /// </summary>
        public static ScreenFade Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<ScreenFade>();
                }

                return instance;
            }
        }

        /// <summary>
        /// Washes out and then loads the scene. Further calls are ignored while a
        /// transition is already running, so a double click cannot queue two.
        /// </summary>
        public void FadeToScene(string sceneName)
        {
            if (leaving)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("ScreenFade was asked to load a scene with no name.", this);
                return;
            }

            leaving = true;
            pendingScene = sceneName;
        }

        private void Awake()
        {
            instance = this;
            alpha = 1f;
            Apply();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            // Unscaled, so a paused game still transitions.
            float dt = Time.unscaledDeltaTime;

            if (leaving)
            {
                alpha = Mathf.MoveTowards(alpha, 1f, dt / Mathf.Max(0.0001f, MenuTheme.FadeOutDuration));
                Apply();

                if (alpha >= 1f)
                {
                    SceneManager.LoadScene(pendingScene);
                }

                return;
            }

            if (alpha <= 0f)
            {
                return;
            }

            alpha = Mathf.MoveTowards(alpha, 0f, dt / Mathf.Max(0.0001f, MenuTheme.FadeInDuration));
            Apply();
        }

        private void Apply()
        {
            if (sheet == null)
            {
                return;
            }

            Color colour = MenuTheme.FadeSheet;
            colour.a = alpha;
            sheet.color = colour;

            // Transparent and idle means the sheet must not intercept clicks;
            // opaque or moving means it should.
            sheet.raycastTarget = leaving || alpha > 0.001f;
        }
    }
}
