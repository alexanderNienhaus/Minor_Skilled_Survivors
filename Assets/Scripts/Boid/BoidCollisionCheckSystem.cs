using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public partial struct BoidCollisionCheckSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer ecb = new (Allocator.TempJob);

        BoidCollisionCheckJob triggerJob = new ()
        {
            allBoids = systemState.GetComponentLookup<Boid>(true),
            allAttackables = systemState.GetComponentLookup<Attackable>(true),
            ecb = ecb,
            em = systemState.EntityManager
        };

        systemState.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), systemState.Dependency);
        systemState.Dependency.Complete();
        ecb.Playback(systemState.EntityManager);
    }
}