using UnityEngine;
using TMPro;

public class ResourceDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text;

    private void OnEnable()
    {
        EventBus<OnResourceChangedEvent>.OnEvent += OnResource;
    }

    private void OnDisable()
    {
        EventBus<OnResourceChangedEvent>.OnEvent -= OnResource;
    }

    private void OnResource(OnResourceChangedEvent pOnBaseHPEvent)
    {
        textField.SetText(text + " " + pOnBaseHPEvent.resource);
    }
}
