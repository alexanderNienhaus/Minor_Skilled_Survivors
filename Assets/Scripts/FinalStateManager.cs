using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalStateManager : MonoBehaviour
{
    [SerializeField] private string wonScene;
    [SerializeField] private string lostScene;
    private float currentWaitTime = 0;
    private int maxWaitTime = 0;
    private OnEndGameEvent onEndGameEvent;

    private void OnEnable()
    {
        EventBus<OnEndGameEvent>.OnEvent += OnEndGame;
        onEndGameEvent = null;
    }

    private void OnDisable()
    {
        EventBus<OnEndGameEvent>.OnEvent -= OnEndGame;
    }

    private void Update()
    {
        if (onEndGameEvent != null)
        {
            currentWaitTime += Time.deltaTime;
            if (currentWaitTime > maxWaitTime)
            {
                SceneManager.LoadScene(onEndGameEvent.won ? wonScene : lostScene);
            }
        }
    }

    private void OnEndGame(OnEndGameEvent pOnEndGameEvent)
    {
        onEndGameEvent = pOnEndGameEvent;
    }
}
