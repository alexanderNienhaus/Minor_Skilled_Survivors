using UnityEngine;
using TMPro;

public class WaveNumberDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text;

    private void OnEnable()
    {
        EventBus<OnWaveNumberChangedEvent>.OnEvent += OnWaveNumberChanged;
    }

    private void OnDisable()
    {
        EventBus<OnWaveNumberChangedEvent>.OnEvent -= OnWaveNumberChanged;
    }

    private void OnWaveNumberChanged(OnWaveNumberChangedEvent pOnWaveNumberChangedEvent)
    {
         textField.SetText(text + " " + pOnWaveNumberChangedEvent.waveNumber);
    }
}
