using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial class BaseSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach (RefRO<Attackable> attackable in SystemAPI.Query<RefRO<Attackable>>().WithAll<Base>())
        {
            EventBus<OnBaseHPEvent>.Publish(new OnBaseHPEvent(attackable.ValueRO.currentHp));
        }
    }
}