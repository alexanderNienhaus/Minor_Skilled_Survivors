using UnityEngine;
using TMPro;

public class BaseHPDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text;

    private void OnEnable()
    {
        EventBus<OnBaseHPEvent>.OnEvent += OnBaseHP;
    }

    private void OnDisable()
    {
        EventBus<OnBaseHPEvent>.OnEvent -= OnBaseHP;
    }

    private void OnBaseHP(OnBaseHPEvent pOnBaseHPEvent)
    {
        textField.SetText(text + " " + pOnBaseHPEvent.baseHP);
    }
}
