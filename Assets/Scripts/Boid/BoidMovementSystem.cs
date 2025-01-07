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

        CountBoids(out NativeArray<Boid> boidArray, out NativeArray<LocalTransform> boidLocalTransformArray, ref pSystemState);

        BoidMovementJob boidMovementJob = new ()
        {
            allBoids = boidArray,
            allBoidLocalTransforms = boidLocalTransformArray,
            collisionWorld = collisionWorld,
            boidSettings = boidSettings,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        boidMovementJob.ScheduleParallel();
    }

    [BurstCompile]
    private void CountBoids(out NativeArray<Boid> boidArray, out NativeArray<LocalTransform> boidLocalTransformArray, ref SystemState pSystemState)
    {
        EntityQueryBuilder entityQueryBuilder = new(Allocator.Temp);
        entityQueryBuilder.WithAll<Boid>();
        int count = pSystemState.GetEntityQuery(entityQueryBuilder).CalculateEntityCount();
        entityQueryBuilder.Dispose();

        int i = 0;
        boidArray = new NativeArray<Boid>(count, Allocator.Persistent);
        boidLocalTransformArray = new NativeArray<LocalTransform>(count, Allocator.Persistent);
        foreach ((RefRW<Boid> boid, RefRW<LocalTransform> localTransform)
            in SystemAPI.Query<RefRW<Boid>, RefRW<LocalTransform>>())
        {
            boidArray[i] = boid.ValueRO;
            boidLocalTransformArray[i] = localTransform.ValueRO;
            i++;
        }
    }
}

