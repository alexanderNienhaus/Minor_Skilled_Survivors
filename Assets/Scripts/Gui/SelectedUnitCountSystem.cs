using UnityEngine;
using Unity.Entities;

[UpdateAfter(typeof(UnitSelectionSystem))]
public partial class SelectedUnitCountSystem : SystemBase
{
    private int count;
    
    public int GetSelectedUnitCount()
    {
        return count;
    }

    protected override void OnUpdate()
    {
        int c = 0;
        foreach (RefRO<SelectedUnitTag> selectedTag in SystemAPI.Query<RefRO<SelectedUnitTag>>())
        {
            c++;
        }
        count = c;
        EventBus<OnSelectedUnitCountChangeEvent>.Publish(new OnSelectedUnitCountChangeEvent(count));
    }
}
