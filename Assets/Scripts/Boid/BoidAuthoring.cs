using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class BoidAuthoring : MonoBehaviour
{
    private class Baker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Boid
            {
            });
        }
    }
}

[BurstCompile]
public struct Boid : IComponentData
{
    public int id;
    public Entity target;
    public float3 targetPosition;
    public float3 velocity;
    public float3 avgFlockHeading;
    public float3 avgAvoidanceHeading;
    public float3 centreOfFlockmates;
    public int numPerceivedFlockmates;
    public int dmg;
    public float strikeDistance;
}