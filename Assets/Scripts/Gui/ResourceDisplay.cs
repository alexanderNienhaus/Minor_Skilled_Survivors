using UnityEngine;
using TMPro;

public class ResourceDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text;

    private void OnEnable()
    {
        EventBus<OnResourceChangedUIEvent>.OnEvent += OnResourceChangedUI;
    }

    private void OnDisable()
    {
        EventBus<OnResourceChangedUIEvent>.OnEvent -= OnResourceChangedUI;
    }

    private void OnResourceChangedUI(OnResourceChangedUIEvent pOnResourceChangedUIEvent)
    {
        textField.SetText(text + " " + pOnResourceChangedUIEvent.resource);
    }
}
