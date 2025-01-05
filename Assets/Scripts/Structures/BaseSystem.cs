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
            float hp = attackable.ValueRO.currentHp;
            EventBus<OnBaseHPEvent>.Publish(new OnBaseHPEvent(hp));
            if (hp <= 0)
            {
                EventBus<OnEndGameEvent>.Publish(new OnEndGameEvent(false));
            }
        }
    }
}