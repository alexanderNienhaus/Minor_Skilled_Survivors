using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial class UnitInformationBridgeSystem : SystemBase
{
    private int friendlyUnitCount;
    private int enemyUnitCount;

    protected override void OnCreate()
    {
        RequireForUpdate<FriendlyUnitCount>();
        RequireForUpdate<EnemyUnitCount>();
    }

    protected override void OnUpdate()
    {
        friendlyUnitCount = SystemAPI.GetSingleton<FriendlyUnitCount>().count;
        enemyUnitCount = SystemAPI.GetSingleton<EnemyUnitCount>().count;
    }

    public int GetFriendlyUnitCount()
    {
        return friendlyUnitCount;
    }

    public int GetEnemyUnitCount()
    {
        return enemyUnitCount;
    }
}
