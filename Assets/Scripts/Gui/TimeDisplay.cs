using UnityEngine;
using TMPro;

public class TimeDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string activeText;
    [SerializeField] private string inactiveText;

    private void OnEnable()
    {
        EventBus<OnTimeChangedEvent>.OnEvent += OnTimeChanged;
    }

    private void OnDisable()
    {
        EventBus<OnTimeChangedEvent>.OnEvent -= OnTimeChanged;
    }

    private void OnTimeChanged(OnTimeChangedEvent pOnTimeChangedEvent)
    {
        if (pOnTimeChangedEvent.isActive)
        {
            textField.SetText(activeText + " " + pOnTimeChangedEvent.time);
        }
        else
        {
            textField.SetText(inactiveText);
        }
    }
}
