using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupSceneManager : MonoBehaviour
{
    private void Start()
    {
        // Go directly to scene A after the loading screen has been created:
        SceneManager.LoadScene("SceneA");
    }
}