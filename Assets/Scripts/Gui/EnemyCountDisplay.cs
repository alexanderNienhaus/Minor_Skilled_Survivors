using UnityEngine;
using Unity.Entities;
using TMPro;

public class EnemyCountDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text = "Enemey unit count: ";

    private void Update()
    {
        textField.SetText(text + World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UnitInformationBridgeSystem>().GetEnemyUnitCount());
    }
}
