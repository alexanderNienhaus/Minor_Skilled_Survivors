using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial class UnitAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<AttackableEnemy>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRO<Attacking> attacking, RefRO<Soldier> soldier, Entity entity) in SystemAPI.Query<RefRO<Attacking>, RefRO<Soldier>>().WithEntityAccess())
        {

        }
    }
}