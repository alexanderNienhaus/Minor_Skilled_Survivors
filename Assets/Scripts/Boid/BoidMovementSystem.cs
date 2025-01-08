using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public partial struct BoidMovementSystem : ISystem
{
    private CollisionWorld collisionWorld;
    private BoidSettings boidSettings;

    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<Boid>();
        pSystemState.RequireForUpdate<BoidSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();
        collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;

        GetBoidEntityArray(out NativeArray<Entity> boidEntityArray, ref pSystemState);

        BoidMovementJob boidMovementJob = new ()
        {
            allBoidEntities = boidEntityArray,
            allLocalTransforms = pSystemState.GetComponentLookup<LocalTransform>(true),
            allBoids = pSystemState.GetComponentLookup<Boid>(true),
            collisionWorld = collisionWorld,
            boidSettings = boidSettings,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        boidMovementJob.ScheduleParallel();
    }

    [BurstCompile]
    private void GetBoidEntityArray(out NativeArray<Entity> pBoidEntityArray, ref SystemState pSystemState)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Boid>();
        EntityQuery query = pSystemState.GetEntityQuery(entityQueryDesc);
        pBoidEntityArray = query.ToEntityArray(Allocator.Persistent);
        entityQueryDesc.Dispose();
    }
}

