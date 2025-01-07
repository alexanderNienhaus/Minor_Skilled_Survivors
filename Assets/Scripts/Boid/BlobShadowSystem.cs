using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
public partial class BlobShadowSystem : SystemBase
{

    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<BlobShadow>())
        {
        }
    }
}