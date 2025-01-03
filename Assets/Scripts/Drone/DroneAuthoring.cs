using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class DroneAuthoring : MonoBehaviour
{
    private class Baker : Baker<DroneAuthoring>
    {
        public override void Bake(DroneAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Drone
            {
                hasTarget = false
            });
        }
    }
}

[BurstCompile]
public struct Drone : IComponentData
{
    public bool hasTarget;
}

