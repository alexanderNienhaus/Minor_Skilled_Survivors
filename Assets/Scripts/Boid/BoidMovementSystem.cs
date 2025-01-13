using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public partial struct BoidMovementSystem : ISystem
{
    private EntityQuery query;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<BoidSettings>();
        pSystemState.RequireForUpdate<Boid>();

        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<LocalTransform, Boid>();
        query = pSystemState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        BoidMovementJob boidMovementJob = new()
        {
            allBoids = query.ToComponentDataArray<Boid>(Allocator.TempJob),
            allLocalTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob),
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
            boidSettings = SystemAPI.GetSingleton<BoidSettings>(),
            deltaTime = SystemAPI.Time.DeltaTime
        };
        pSystemState.Dependency = boidMovementJob.ScheduleParallel(pSystemState.Dependency);
    }
}

