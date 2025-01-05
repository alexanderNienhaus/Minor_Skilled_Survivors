using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalStateManager : MonoBehaviour
{
    [SerializeField] private string wonScene;
    [SerializeField] private string lostScene;

    private void OnEnable()
    {
        EventBus<OnEndGameEvent>.OnEvent += OnEndGame;
    }

    private void OnDisable()
    {
        EventBus<OnEndGameEvent>.OnEvent -= OnEndGame;
    }

    private void OnEndGame(OnEndGameEvent pOnEndGameEvent)
    {
        SceneManager.LoadScene(pOnEndGameEvent.won ? wonScene : lostScene);
    }
}
