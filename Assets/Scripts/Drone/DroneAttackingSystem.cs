using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial class DroneAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        if (!CountUnits(out NativeArray<Entity> entityUnitArray))
            return;

        DroneAttackingJob droneAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(),
            allUnitEntities = entityUnitArray,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = droneAttackingJob.ScheduleParallel(Dependency);
        beginFixedStepSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private bool CountUnits(out NativeArray<Entity> pEntityUnitArray)
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform) },
            None = new ComponentType[] { typeof(Drone), typeof(Boid) }
        };
        int count = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

        int i = 0;
        pEntityUnitArray = new NativeArray<Entity>(count, Allocator.Persistent);
        foreach ((RefRO<Attackable> boid, RefRO<LocalTransform> localTransform, Entity entity)
            in SystemAPI.Query<RefRO<Attackable>, RefRO<LocalTransform>>().WithEntityAccess().WithNone<Drone, Boid>())
        {
            pEntityUnitArray[i] = entity;
            i++;
        }

        if (i == 0)
            return false;

        return true;
    }
}