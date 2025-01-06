using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
public partial class AATurretAttackingSystem : SystemBase
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

        if (!CountEnemies(out NativeArray<Entity> entityEnemyArray))
            return;

        AATurretAttackingJob aaTurretAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            entityManager = EntityManager,
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(),
            allPhysicsVelocities = GetComponentLookup<PhysicsVelocity>(),
            allEntityEnemies = entityEnemyArray,
            ressource = SystemAPI.GetSingletonRW<Ressource>(),
            deltaTime = SystemAPI.Time.DeltaTime            
        };
        Dependency = aaTurretAttackingJob.ScheduleParallel(Dependency);
        beginFixedStepSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private bool CountEnemies(out NativeArray<Entity> pEntityEnemyArray)
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
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

        if (i == 0)
            return false;

        return true;
    }
}