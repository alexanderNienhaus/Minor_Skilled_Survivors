using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
public partial class AATurretAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
        RequireForUpdate<AATurret>();
        RequireForUpdate<Boid>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();
        
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
    private void GetEnemyEntityArray(out NativeArray<Entity> pEntityEnemyArray)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable, LocalTransform, PhysicsVelocity>().WithAny<Boid>();
        EntityQuery query = GetEntityQuery(entityQueryDesc);
        pEntityEnemyArray = query.ToEntityArray(Allocator.Persistent);
        entityQueryDesc.Dispose();
    }
}