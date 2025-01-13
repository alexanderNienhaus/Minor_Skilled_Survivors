using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial class BaseSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach (RefRO<Attackable> attackableBase in SystemAPI.Query<RefRO<Attackable>>().WithAll<Base>())
        {
            float hp = attackableBase.ValueRO.currentHp;
            if (hp < 0)
            {
                hp = 0;
            }

            EventBus<OnBaseHPEvent>.Publish(new OnBaseHPEvent(math.ceil(hp)));

            if (hp > 0)
                continue;

            EventBus<OnEndGameEvent>.Publish(new OnEndGameEvent(false));
        }
    }
}