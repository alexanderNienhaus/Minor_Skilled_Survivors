using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

public class AATurretAuthoring : MonoBehaviour
{
    [SerializeField] private float turnSpeed;

    private class Baker : Baker<AATurretAuthoring>
    {
        public override void Bake(AATurretAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AATurret
            {
                turnSpeed = pAuthoring.turnSpeed
            });
        }
    }
}

[BurstCompile]
public struct AATurret : IComponentData
{
    public float turnSpeed;
}

