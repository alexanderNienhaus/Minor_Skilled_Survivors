using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;

    public void Load()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
