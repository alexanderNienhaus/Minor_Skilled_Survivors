using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class BoidTargetAuthoring : MonoBehaviour
{
    [SerializeField] private int priotity;

    private class Baker : Baker<BoidTargetAuthoring>
    {
        public override void Bake(BoidTargetAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BoidTarget
            {
                priority = pAuthoring.priotity
            });
        }
    }
}

[BurstCompile]
public struct BoidTarget : IComponentData
{
    public int priority;
}
