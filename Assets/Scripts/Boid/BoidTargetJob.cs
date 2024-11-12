using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
public partial struct BoidTargetJob : IJobEntity
{
    public BoidTarget target;
    public LocalTransform localTransformTarget;

    [BurstCompile]
    public void Execute(ref Boid boid)
    {
        boid.target = localTransformTarget;
    }
}
