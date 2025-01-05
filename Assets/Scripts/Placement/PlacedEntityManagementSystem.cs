using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial class PlacedEntityManagementSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;
    private EntityCommandBuffer ecb;
    private Vector3 worldPos;
    private quaternion rotation;
    private float scale;
    private int index;
    private int id;
    private bool spawn;
    private bool destroy;

    protected override void OnCreate()
    {
        RequireForUpdate<PlacableEntityBuffer>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        spawn = false;
        destroy = false;
    }

    protected override void OnUpdate()
    {
        ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        if (spawn)
        {
            foreach (DynamicBuffer<PlacableEntityBuffer> gridObjectSpawner in SystemAPI.Query<DynamicBuffer<PlacableEntityBuffer>>())
            {
                Entity placedEntity = ecb.Instantiate(gridObjectSpawner[index].prefab);
                ecb.SetComponent(placedEntity, new LocalTransform
                {
                    Position = worldPos,
                    Rotation = rotation,
                    Scale = scale
                });
                ecb.AddComponent<Parent>(placedEntity);
                ecb.SetComponent(placedEntity, new Parent { Value = gridObjectSpawner[index].parent });
                ecb.SetComponent(placedEntity, new PlacedObjectTag { id = id });
            }
            spawn = false;
        }

        if (destroy)
        {
            foreach ((RefRO<PlacedObjectTag> placedObjectTag, Entity entity) in SystemAPI.Query<RefRO<PlacedObjectTag>>().WithEntityAccess())
            {
                if (placedObjectTag.ValueRO.id == id)
                {
                    ecb.DestroyEntity(entity);
                    break;
                }
            }
            destroy = false;
        }
    }

    public void CreateEntity(int pIndex, Vector3 pWorldPos, quaternion pRotation, float pScale, int pId)
    {
        worldPos = pWorldPos;
        rotation = pRotation;
        scale = pScale;
        index = pIndex;
        id = pId;
        spawn = true;
    }

    public void DestroyEntity(int pId)
    {
        id = pId;
        destroy = true;
    }
}
