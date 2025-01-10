using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class UnitInformationAuthoring : MonoBehaviour
{
    private class Baker : Baker<UnitInformationAuthoring>
    {
        public override void Bake(UnitInformationAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new FriendlyUnitCount
            {
            });

            AddComponent(entity, new EnemyUnitCount
            {
            });

            AddComponent(entity, new GroupPosition
            {
            });
        }
    }
}

[BurstCompile]
public struct FriendlyUnitCount : IComponentData
{
    public int count;
}


[BurstCompile]
public struct EnemyUnitCount : IComponentData
{
    public int count;
}

[BurstCompile]
public struct GroupPosition : IComponentData
{
    public float3 pos;
}