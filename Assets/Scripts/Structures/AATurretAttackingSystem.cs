using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
public partial class AATurretAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        CountEnemies(out NativeArray<Entity> entityEnemyArray);

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
    private void CountEnemies(out NativeArray<Entity> pEntityEnemyArray)
    {
        EntityQueryDesc entityQueryDesc = new ()
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform), typeof(PhysicsVelocity) },
            Any = new ComponentType[] { typeof(Boid) }
        };
        int count = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

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