using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.UI
{
    /// <summary>
    /// One way to leave a screen: through the fade when the screen has one, and
    /// straight to the load when it does not.
    ///
    /// Split out of <see cref="LoadSceneButton"/> once something other than a
    /// button needed to change scenes. The rule it holds is small but easy to
    /// get half right -- a second caller that forgot the fade would make the
    /// tutorial cut where every other screen washes.
    /// </summary>
    public static class SceneRouter
    {
        /// <summary>
        /// Leaves for another scene, fading first when a <see cref="ScreenFade"/>
        /// is present. The scene must be in Build Settings.
        /// </summary>
        public static void Go(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneRouter was asked to load a scene with no name.");
                return;
            }

            if (ScreenFade.Instance != null)
            {
                ScreenFade.Instance.FadeToScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
