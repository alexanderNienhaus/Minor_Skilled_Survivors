using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
public partial struct InitializeBoidsJob : IJobEntity
{
    public BoidSettings boidSettings;

    [BurstCompile]
    public void Execute(ref Boid boid, in LocalTransform localTransform)
    {
        float3 startSpeed = (boidSettings.minSpeed + boidSettings.maxSpeed) / 2;
        boid.velocity = localTransform.Forward() * startSpeed;
    }
}
