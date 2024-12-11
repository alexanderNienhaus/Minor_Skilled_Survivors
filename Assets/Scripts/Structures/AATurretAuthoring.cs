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
            DynamicBuffer<LinkedEntityGroup> children = World.DefaultGameObjectInjectionWorld.EntityManager.GetBuffer<LinkedEntityGroup>(entity);
            LocalTransform mount = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(children.ElementAt(2).Value);
            LocalTransform head = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(children.ElementAt(5).Value);

            AddComponent(entity, new AATurret
            {
                head = head,
                mount = mount,
                turnSpeed = pAuthoring.turnSpeed
            });
        }
    }
}

[BurstCompile]
public struct AATurret : IComponentData
{
    public LocalTransform head;
    public LocalTransform mount;
    public float turnSpeed;
}

