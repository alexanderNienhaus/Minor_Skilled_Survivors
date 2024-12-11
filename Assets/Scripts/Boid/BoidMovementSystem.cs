using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

public partial class BoidMovementSystem : SystemBase
{
    private CollisionWorld collisionWorld;
    private BoidSettings boidSettings;

    protected override void OnCreate()
    {
        RequireForUpdate<Boid>();
        RequireForUpdate<BoidSettings>();
    }

    protected override void OnUpdate()
    {
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();
        collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;

        NativeArray<Boid> boidArray;
        NativeArray<LocalTransform> boidLocalTransformArray;
        CountBoids(out boidArray, out boidLocalTransformArray, World.GetExistingSystemManaged<WaveSystem>().currentNumberOfBoids);

        ComputeBoidsJob computeBoidsJob = new ComputeBoidsJob
        {
            allBoids = boidArray,
            allBoidLocalTransforms = boidLocalTransformArray,
            collisionWorld = collisionWorld,
            boidSettings = boidSettings,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        computeBoidsJob.ScheduleParallel();
    }

    private void CountBoids(out NativeArray<Boid> boidArray, out NativeArray<LocalTransform> boidLocalTransformArray, int spawnCount)
    {
        int i = 0;
        boidArray = new NativeArray<Boid>(spawnCount + 1, Allocator.Persistent);
        boidLocalTransformArray = new NativeArray<LocalTransform>(spawnCount + 1, Allocator.Persistent);
        foreach ((RefRW<Boid> boid, RefRW<LocalTransform> localTransform)
            in SystemAPI.Query<RefRW<Boid>, RefRW<LocalTransform>>())
        {
            boidArray[i] = boid.ValueRO;
            boidLocalTransformArray[i] = localTransform.ValueRO;
            i++;
        }
    }
}

