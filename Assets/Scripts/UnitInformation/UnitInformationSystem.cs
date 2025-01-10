using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct UnitInformationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState pState)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pState)
    {
        /*
        int enemyUnitCount = GetEnemyUnitCount(ref pState);
        SystemAPI.GetSingletonRW<EnemyUnitCount>().ValueRW.count = enemyUnitCount;

        int friendlyUnitCount = GetFriendlyUnitCount(ref pState);
        SystemAPI.GetSingletonRW<FriendlyUnitCount>().ValueRW.count = friendlyUnitCount;

        float3 groupPos = GetSelectedUnitGroupPosition(friendlyUnitCount, ref pState);
        SystemAPI.GetSingletonRW<GroupPosition>().ValueRW.pos = groupPos;
        */
    }

    private float3 GetSelectedUnitGroupPosition(int friendlyUnitCount, ref SystemState pState)
    {
        if (friendlyUnitCount == 0)
            return float3.zero;

        float3 cumulativePos = float3.zero;
        foreach ((RefRO<PathFollow> pathFollow, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRO<PathFollow>, RefRO<LocalTransform>>().WithAll<SelectedUnitTag>())
        {
            cumulativePos += localTransform.ValueRO.Position;
        }

         return cumulativePos / friendlyUnitCount;
    }

    private int GetEnemyUnitCount(ref SystemState pState)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAny<Boid, Drone>();
        EntityQuery query = pState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
        return query.CalculateEntityCount();
    }

    private int GetFriendlyUnitCount(ref SystemState pState)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable>().WithNone<Boid, Drone>();
        EntityQuery query = pState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
        return query.CalculateEntityCount();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState pState)
    {
        
    }
}
