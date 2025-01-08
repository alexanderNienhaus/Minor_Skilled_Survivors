using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DroneAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;
    private int enemyCount;

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();

        CountUnits(out NativeArray<Entity> entityUnitArray);

        DroneAttackingJob droneAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            em = EntityManager,
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(true),
            allUnitEntities = entityUnitArray,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = droneAttackingJob.ScheduleParallel(Dependency);
        beginSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private void CountUnits(out NativeArray<Entity> pEntityUnitArray)
    {
        EntityQueryDesc entityQueryDesc = new ()
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform) },
            None = new ComponentType[] { typeof(Drone), typeof(Boid) }
        };
        enemyCount = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

        int i = 0;
        pEntityUnitArray = new NativeArray<Entity>(enemyCount, Allocator.Persistent);
        foreach ((RefRO<Attackable> boid, RefRO<LocalTransform> localTransform, Entity entity)
            in SystemAPI.Query<RefRO<Attackable>, RefRO<LocalTransform>>().WithEntityAccess().WithNone<Drone, Boid>())
        {
            if (!EntityManager.Exists(entity))
                continue;

            pEntityUnitArray[i] = entity;
            i++;
        }
    }
}