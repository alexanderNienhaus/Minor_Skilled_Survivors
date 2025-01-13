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
        allBoids = pSystemState.GetComponentLookup<Boid>(true);
        allAttackables = pSystemState.GetComponentLookup<Attackable>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer ecb = new (Allocator.TempJob);
        allBoids.Update(ref systemState);
        allAttackables.Update(ref systemState);

        BoidCollisionCheckJob triggerJob = new ()
        {
            allBoids = allBoids,
            allAttackables = allAttackables,
            ecb = ecb,
            em = systemState.EntityManager
        };

        systemState.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), systemState.Dependency);
        systemState.Dependency.Complete();
        ecb.Playback(systemState.EntityManager);
    }
}