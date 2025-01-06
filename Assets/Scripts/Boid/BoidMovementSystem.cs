using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public partial class BoidMovementSystem : SystemBase
{
    private CollisionWorld collisionWorld;
    private BoidSettings boidSettings;

    protected override void OnCreate()
    {
        RequireForUpdate<Boid>();
        RequireForUpdate<BoidSettings>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();
        collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;

        NativeArray<Boid> boidArray;
        NativeArray<LocalTransform> boidLocalTransformArray;
        CountBoids(out boidArray, out boidLocalTransformArray);

        ComputeBoidsJob computeBoidsJob = new ()
        {
            allBoids = boidArray,
            allBoidLocalTransforms = boidLocalTransformArray,
            collisionWorld = collisionWorld,
            boidSettings = boidSettings,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        computeBoidsJob.ScheduleParallel();
    }

    [BurstCompile]
    private void CountBoids(out NativeArray<Boid> boidArray, out NativeArray<LocalTransform> boidLocalTransformArray)
    {
        EntityQueryDesc entityQueryDesc = new ()
        {
            All = new ComponentType[] { typeof(Boid) },
        };
        int count = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

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

