using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(UnitSelectionSystem))]
public partial class SelectedUnitInformationSystem : SystemBase
{
    private int count;
    private float3 groupStartPos;

    public int GetSelectedUnitCount()
    {
        return count;
    }

    public float3 GetGroupStartPos()
    {
        return groupStartPos;
    }

    protected override void OnCreate()
    {
        RequireForUpdate<SelectedUnitTag>();
    }

    public void UpdateSelectedUnitInfo()
    {
        int c = 0;
        float3 cumulativePos = float3.zero;
        foreach ((RefRO<SelectedUnitTag> selectedTag, RefRO<PathFollow> pathFollow, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRO<SelectedUnitTag>, RefRO<PathFollow>, RefRO<LocalTransform>>())
        {
            c++;
            cumulativePos += localTransform.ValueRO.Position;
        }
        groupStartPos = cumulativePos / c;
        count = c;
        EventBus<OnSelectedUnitCountChangeEvent>.Publish(new OnSelectedUnitCountChangeEvent(count));
    }

    protected override void OnUpdate()
    {
    }
}
