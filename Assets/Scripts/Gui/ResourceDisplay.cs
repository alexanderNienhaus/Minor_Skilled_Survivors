using UnityEngine;
using TMPro;

public class ResourceDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text;

    private void OnEnable()
    {
        EventBus<OnResourceChangedUIEvent>.OnEvent += OnResource;
    }

    private void OnDisable()
    {
        EventBus<OnResourceChangedUIEvent>.OnEvent -= OnResource;
    }

    private void OnResource(OnResourceChangedUIEvent pOnResourceChangedEvent)
    {
        textField.SetText(text + " " + pOnResourceChangedEvent.resource);
    }
}
