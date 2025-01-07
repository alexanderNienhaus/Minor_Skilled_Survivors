using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial class BaseSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        foreach ((RefRO<Attackable> attackable, Entity entity) in SystemAPI.Query<RefRO<Attackable>>().WithEntityAccess().WithAll<Base>())
        {
            float hp = attackable.ValueRO.currentHp;
            if (hp < 0)
            {
                hp = 0;
            }

            EventBus<OnBaseHPEvent>.Publish(new OnBaseHPEvent(hp));
            if (hp <= 0)
            {
                EventBus<OnEndGameEvent>.Publish(new OnEndGameEvent(false));
                ecb.DestroyEntity(entity);
            }
        }
    }
}