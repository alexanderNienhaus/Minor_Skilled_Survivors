using TMPro;
using UnityEngine;

public class InfoMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private int maxTimeToLive;
    private float currentTimeToLive;

    private void OnEnable()
    {
        EventBus<OnInfoMenuTextChangeEvent>.OnEvent += OnInfoMenuTextChange;
    }

    private void OnDisable()
    {
        EventBus<OnInfoMenuTextChangeEvent>.OnEvent -= OnInfoMenuTextChange;
    }

    private void OnInfoMenuTextChange(OnInfoMenuTextChangeEvent pOnInfoMenuTextChangeEvent)
    {
        textField.SetText(pOnInfoMenuTextChangeEvent.text);
        currentTimeToLive = maxTimeToLive;
    }

    private void Update()
    {
        if (currentTimeToLive <= 0)
        {
            textField.SetText("");
        } else
        {
            currentTimeToLive -= Time.deltaTime;
        }
    }
}
