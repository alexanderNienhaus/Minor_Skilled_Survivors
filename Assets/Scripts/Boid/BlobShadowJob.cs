using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
[WithAll(typeof(Boid))]
public partial struct BlobShadowJob : IJobEntity
{
    [BurstCompile]
    public void Execute(in LocalTransform pLocalTransformBoid)
    {

    }
}