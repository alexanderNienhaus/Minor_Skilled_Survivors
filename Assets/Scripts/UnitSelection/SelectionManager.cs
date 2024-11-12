using UnityEngine;
using Unity.Entities;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private Color borderColor = Color.blue;
    [SerializeField] private Color fillColor = new Color(0.8f, 0.8f, 0.9f, 0.2f);

    private UnitSelectionSystem unitSelectionSystem;

    private void Start()
    {
        unitSelectionSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UnitSelectionSystem>();
    }

    private void OnGUI()
    {
        if (unitSelectionSystem.GetIsDragging())
        {
            Rect rect = SelectionGUI.GetScreenRect(unitSelectionSystem.GetMouseStartPos(), Input.mousePosition);
            SelectionGUI.DrawScreenRect(rect, fillColor);
            SelectionGUI.DrawScreenRectBorder(rect, 1, borderColor);
        }
    }
}
