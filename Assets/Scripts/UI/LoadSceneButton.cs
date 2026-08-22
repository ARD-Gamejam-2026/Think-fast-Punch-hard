using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.UI
{
    /// <summary>
    /// Sends a button to another scene, through the fade when there is one.
    ///
    /// The click is subscribed in code rather than through the Inspector's
    /// UnityEvent list, because these screens are generated: a persistent call
    /// wired from an editor script is awkward to serialise and invisible in a
    /// diff, whereas a component with a scene name on it says what it does in
    /// both places.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LoadSceneButton : MonoBehaviour
    {
        [Tooltip("Scene to load. Must be in Build Settings.")]
        [SerializeField] private string sceneName;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Go);
        }

        private void Go()
        {
            if (MenuSounds.Instance != null)
            {
                MenuSounds.Instance.PlayPress();
            }

            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeToScene(sceneName);
                return;
            }

            // No fade in this screen: still go, just without the wash.
            SceneManager.LoadScene(sceneName);
        }
    }
}
