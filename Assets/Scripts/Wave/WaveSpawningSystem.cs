using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public partial class WaveSpawningSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        //RequireForUpdate<BoidSpawning>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        Enabled = false;

        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach (RefRO<WaveSpawner> waveSpawner in SystemAPI.Query<RefRO<WaveSpawner>>())
        {
            Random r = new Random((uint)(waveSpawner.ValueRO.amount + 1));
            for (int i = 0; i < waveSpawner.ValueRO.amount; i++)
            {
                float2 randomDistance = r.NextFloat2Direction() * waveSpawner.ValueRO.radius;
                float3 pos = waveSpawner.ValueRO.position + new float3(randomDistance.x, 0, randomDistance.y);
                Entity entity = ecb.Instantiate(waveSpawner.ValueRO.prefab);
                ecb.SetComponent(entity, new LocalTransform
                {
                    Position = pos,
                    Rotation = quaternion.identity,
                    Scale = 1
                });
                //ecb.SetComponent(entity, new Boid { id = i });
                ecb.AddComponent<Parent>(entity);
                ecb.SetComponent(entity, new Parent { Value = waveSpawner.ValueRO.parent });
            }
        }
    }
}