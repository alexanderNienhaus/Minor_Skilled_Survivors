using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class DronePathFindingAuthoring : MonoBehaviour
{
    [SerializeField] private Vector3 target;

    private class Baker : Baker<DronePathFindingAuthoring>
    {
        public override void Bake(DronePathFindingAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new DronePathFinding
            {
                target = pAuthoring.target
            });
        }
    }
}

[BurstCompile]
public struct DronePathFinding : IComponentData, IEnableableComponent
{
    public float3 target;
}


