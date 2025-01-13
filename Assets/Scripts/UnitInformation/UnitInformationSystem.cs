using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct UnitInformationSystem : ISystem
{
    private EntityQuery enemyUnitQuery;
    private EntityQuery friendlyUnitQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<EnemyUnitCount>();
        pSystemState.RequireForUpdate<FriendlyUnitCount>();
        pSystemState.RequireForUpdate<GroupPosition>();

        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAny<Boid, Drone>();
        enemyUnitQuery = pSystemState.GetEntityQuery(entityQueryDesc);

        entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable>().WithNone<Boid, Drone>();
        friendlyUnitQuery = pSystemState.GetEntityQuery(entityQueryDesc);

        entityQueryDesc.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        int enemyUnitCount = GetEnemyUnitCount(ref pSystemState);
        SystemAPI.GetSingletonRW<EnemyUnitCount>().ValueRW.count = enemyUnitCount;

        int friendlyUnitCount = GetFriendlyUnitCount(ref pSystemState);
        SystemAPI.GetSingletonRW<FriendlyUnitCount>().ValueRW.count = friendlyUnitCount;

        float3 groupPos = GetSelectedUnitGroupPosition(friendlyUnitCount, ref pSystemState);
        SystemAPI.GetSingletonRW<GroupPosition>().ValueRW.pos = groupPos;
    }

    private float3 GetSelectedUnitGroupPosition(int friendlyUnitCount, ref SystemState pSystemState)
    {
        if (friendlyUnitCount == 0)
            return float3.zero;

        float3 cumulativePos = float3.zero;
        foreach ((RefRO<PathFollow> pathFollow, RefRO<LocalTransform> localTransform) in SystemAPI.Query<RefRO<PathFollow>, RefRO<LocalTransform>>().WithAll<SelectedUnitTag>())
        {
            cumulativePos += localTransform.ValueRO.Position;
        }

         return cumulativePos / friendlyUnitCount;
    }

    private int GetEnemyUnitCount(ref SystemState pSystemState)
    {
        return enemyUnitQuery.CalculateEntityCount();
    }

    private int GetFriendlyUnitCount(ref SystemState pSystemState)
    {
        return friendlyUnitQuery.CalculateEntityCount();
    }
}
