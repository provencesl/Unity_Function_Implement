using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitioner : MonoBehaviour
{
    [SerializeField]
    private string sceneToTransitionTo;

    public void Transition()
    {
        LoadingScreen.Instance.Show(SceneManager.LoadSceneAsync(sceneToTransitionTo));
    }
}