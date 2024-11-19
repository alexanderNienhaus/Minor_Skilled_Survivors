using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class FormationPositionAuthoring : MonoBehaviour
{
    private class Baker : Baker<FormationPositionAuthoring>
    {
        public override void Bake(FormationPositionAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new FormationPosition
            {
                isSet = false
            });
        }
    }
}

[BurstCompile]
public struct FormationPosition : IComponentData
{
    public float3 position;
    public bool isSet;
}


