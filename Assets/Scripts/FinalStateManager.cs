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
        if (onEndGameEvent == null)
            return;

        currentWaitTime += Time.deltaTime;
        if (currentWaitTime <= maxWaitTime)
            return;

        SceneManager.LoadScene(onEndGameEvent.won ? wonScene : lostScene);
    }

    private void OnEndGame(OnEndGameEvent pOnEndGameEvent)
    {
        onEndGameEvent = pOnEndGameEvent;
    }

    // 1 unit = 500 enemies
    // 1 Tank = 50 Drones
    // 1 AA = 500 Boids

    //      100 - 1000 - 15000       Enemies

    //      1   - 5    - 25          Units
    //      0.2 - 2    - 30          Needed to kill
    //      x5  - x2.5 - x0.8

    //Res       1       0
    //          Drones  Boids   Res
    //Wave 1    100     0       100
    //Wave 2    500     205     500               
    //Wave 3    10000   5000    10000
}
