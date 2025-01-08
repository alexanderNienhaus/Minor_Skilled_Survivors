using UnityEngine;

public class TimeScaleDebug : MonoBehaviour
{
    [Range(0.1f, 10)] [SerializeField] private float timeScale = 1;

    private void Update()
    {
        Time.timeScale = timeScale;
    }
}
