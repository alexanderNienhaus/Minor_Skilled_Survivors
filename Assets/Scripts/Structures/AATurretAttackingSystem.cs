using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
public partial class AATurretAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;
    private int count;

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        
        CountEnemies();
        GetEnemyEntityArray(out NativeArray<Entity> entityEnemyArray);

        AATurretAttackingJob aaTurretAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            entityManager = EntityManager,
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(),
            allPhysicsVelocities = GetComponentLookup<PhysicsVelocity>(),
            allEntityEnemies = entityEnemyArray,
            deltaTime = SystemAPI.Time.DeltaTime            
        };
        Dependency = aaTurretAttackingJob.ScheduleParallel(Dependency);
        beginFixedStepSimulationEcbSystem.AddJobHandleForProducer(Dependency);
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
            pEntityEnemyArray[i] = entity;
            i++;
        }
    }

}