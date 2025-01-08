using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
//[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class AATurretAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;
    private int count;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();
        
        CountEnemies();
        GetEnemyEntityArray(out NativeArray<Entity> entityEnemyArray);

        AATurretAttackingJob aaTurretAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            em = EntityManager,
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(true),
            allPhysicsVelocities = GetComponentLookup<PhysicsVelocity>(true),
            allEntityEnemies = entityEnemyArray,
            resource = SystemAPI.GetSingletonRW<Resource>(),
            deltaTime = SystemAPI.Time.DeltaTime            
        };
        Dependency = aaTurretAttackingJob.ScheduleParallel(Dependency);
        beginSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    public void CountEnemies()
    {
        EntityQueryDesc entityQueryDesc = new ()
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform), typeof(PhysicsVelocity) },
            Any = new ComponentType[] { typeof(Boid) }
        };
        count = GetEntityQuery(entityQueryDesc).CalculateEntityCount();
    }

    [BurstCompile]
    private void GetEnemyEntityArray(out NativeArray<Entity> pEntityEnemyArray)
    {
        int i = 0;
        pEntityEnemyArray = new NativeArray<Entity>(count, Allocator.Persistent);
        foreach ((RefRO<Attackable> boid, RefRO<LocalTransform> localTransform, RefRO<PhysicsVelocity> physicsVelocity, Entity entity)
            in SystemAPI.Query<RefRO<Attackable>, RefRO<LocalTransform>, RefRO<PhysicsVelocity>>().WithEntityAccess().WithAny<Boid>())
        {
            if (!EntityManager.Exists(entity))
                continue;

            pEntityEnemyArray[i] = entity;
            i++;
        }
    }

}