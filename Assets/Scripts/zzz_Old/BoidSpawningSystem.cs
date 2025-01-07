using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public partial class BoidSpawningSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<BoidSpawning>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        Enabled = false;

        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach (RefRO<BoidSpawning> boidSpawning in SystemAPI.Query<RefRO<BoidSpawning>>())
        {
            for (int i = 0; i < boidSpawning.ValueRO.spawnCount; i++)
            {
                Random r = new Random((uint)(boidSpawning.ValueRO.spawnCount * i + 1));
                float3 pos = boidSpawning.ValueRO.spawnPosition + r.NextFloat3Direction() * boidSpawning.ValueRO.spawnRadius;
                Entity boid = ecb.Instantiate(boidSpawning.ValueRO.prefab);
                ecb.SetComponent(boid, new LocalTransform
                {
                    Position = pos,
                    Rotation = quaternion.LookRotationSafe(r.NextFloat3Direction(), LocalTransform.Identity.Up()),
                    Scale = boidSpawning.ValueRO.scale
                });
                ecb.SetComponent(boid, new Boid { id = i });
                ecb.AddComponent<Parent>(boid);
                ecb.SetComponent(boid, new Parent { Value = boidSpawning.ValueRO.parent });
            }
        }
    }
}


