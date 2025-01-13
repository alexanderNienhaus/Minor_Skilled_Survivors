using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

public class AATurretAuthoring : MonoBehaviour
{
    [SerializeField] private float turnSpeed;
    [SerializeField] private int childNumberModel = 1;
    [SerializeField] private int childNumberMount = 3;
    [SerializeField] private int childNumberHead = 6;

    private class Baker : Baker<AATurretAuthoring>
    {
        public override void Bake(AATurretAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AATurret
            {
                turnSpeed = pAuthoring.turnSpeed,
                childNumberModel = pAuthoring.childNumberModel,
                childNumberMount = pAuthoring.childNumberMount,
                childNumberHead = pAuthoring.childNumberHead
            });
        }
    }
}

[BurstCompile]
public struct AATurret : IComponentData
{
    public float turnSpeed;
    public int childNumberModel;
    public int childNumberMount;
    public int childNumberHead;
}

