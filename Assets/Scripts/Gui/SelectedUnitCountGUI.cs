using UnityEngine;
using TMPro;

public class SelectedUnitCountGUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selectedUnitCountText;

    private int selectedUnitCount = 0;
    private string text = "Number of selected units: ";

    private void OnEnable()
    {
        EventBus<OnSelectedUnitCountChangeEvent>.OnEvent += OnSelectedUnitCountChanged;
    }

    private void OnDisable()
    {
        EventBus<OnSelectedUnitCountChangeEvent>.OnEvent -= OnSelectedUnitCountChanged;
    }

    private void Start()
    {
        selectedUnitCountText.SetText(text + selectedUnitCount);
    }

    private void OnSelectedUnitCountChanged(OnSelectedUnitCountChangeEvent pOnSelectedUnitCountChangeEvent)
    {
        selectedUnitCount = pOnSelectedUnitCountChangeEvent.selectedUnitCount;
        selectedUnitCountText.SetText(text + selectedUnitCount);
    }
}
