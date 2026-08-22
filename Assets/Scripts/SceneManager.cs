using UnityEngine;

[CreateAssetMenu(fileName = "SceneManager", menuName = "Scriptable Objects/SceneManager")]
public class SceneManager : ScriptableObject
{
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
