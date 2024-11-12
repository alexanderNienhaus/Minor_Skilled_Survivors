using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

//[CreateAfter(typeof(BoidSystem))]
//[CreateAfter(typeof(BoidSettingsD))]
public partial class BoidMovementSystem : SystemBase
{
    private CollisionWorld collisionWorld;
    private BoidSettings boidSettings;
    private bool firstUpdateCall = true;

    protected override void OnCreate()
    {
        RequireForUpdate<BoidSettings>();
    }

    protected override void OnUpdate()
    {
        RequireForUpdate<BoidSpawning>();
        if (firstUpdateCall)
        {
            FirstUpdate();
            return;
        }
        collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;

        NativeArray<Boid> boidArray;
        NativeArray<LocalTransform> boidLocalTransformArray;
        CountBoids(out boidArray, out boidLocalTransformArray, SystemAPI.GetSingleton<BoidSpawning>().spawnCount);

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

    private void FirstUpdate()
    {
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();

        InitializeBoidsJob initializeBoidsJob = new InitializeBoidsJob
        {
            boidSettings = boidSettings
        };
        initializeBoidsJob.ScheduleParallel();

        firstUpdateCall = false;
    }   
}

