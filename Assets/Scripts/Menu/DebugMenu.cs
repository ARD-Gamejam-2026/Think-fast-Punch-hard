using UnityEngine;
using UnityEngine.InputSystem;

namespace ThinkFast.Menu
{
    public class DebugMenu : MonoBehaviour
    {
        public static DebugMenu Instance;
        [SerializeField] private GameObject debugMenuUI;

        private bool isDebugMenuVisible = false;
        private string sceneName;
        public string SceneName
        {
            get { return sceneName; }
            set { sceneName = value; }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        // Update is called once per frame
        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                ToggleVisible();
            }

            if (isDebugMenuVisible)
            {
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                {
                    switch (sceneName)
                    {
                        case "menu":
                            UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Menu");
                            ToggleVisible();
                            break;
                        case "game":
                            UnityEngine.SceneManagement.SceneManager.LoadScene("PlayerControllerTest");
                            ToggleVisible();
                            break;
                        case "end":
                            UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_End");
                            ToggleVisible();
                            break;
                        case "quiz":
                            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
                            ToggleVisible();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ToggleVisible()
        {
            isDebugMenuVisible = !isDebugMenuVisible;
            debugMenuUI.SetActive(isDebugMenuVisible);

        }

    }
}
