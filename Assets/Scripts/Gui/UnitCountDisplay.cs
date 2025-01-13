using UnityEngine;
using Unity.Entities;
using TMPro;

public class UnitCountDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text = "Friendly unit count: ";

    private void Update()
    {
        textField.SetText(text + World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UnitInformationBridgeSystem>().GetFriendlyUnitCount());
    }
}
