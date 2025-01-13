using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public partial struct BoidCollisionCheckSystem : ISystem
{
    [ReadOnly] private ComponentLookup<Boid> allBoids;
    [ReadOnly] private ComponentLookup<Attackable> allAttackables;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<Boid>();

        allBoids = pSystemState.GetComponentLookup<Boid>(true);
        allAttackables = pSystemState.GetComponentLookup<Attackable>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        EntityCommandBuffer ecb = new (Allocator.TempJob);
        allBoids.Update(ref pSystemState);
        allAttackables.Update(ref pSystemState);

        BoidCollisionCheckJob triggerJob = new ()
        {
            allBoids = allBoids,
            allAttackables = allAttackables,
            ecb = ecb,
            em = pSystemState.EntityManager
        };

        pSystemState.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), pSystemState.Dependency);
        pSystemState.Dependency.Complete();
        ecb.Playback(pSystemState.EntityManager);
        ecb.Dispose();
    }
}