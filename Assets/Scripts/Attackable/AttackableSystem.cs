using Unity.Burst;
using Unity.Entities;

public partial class AttackableSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;
    private RefRW<Resource> resource;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        resource = SystemAPI.GetSingletonRW<Resource>();

        foreach ((RefRW<Attackable> attackable, Entity entity) in SystemAPI.Query<RefRW<Attackable>>().WithEntityAccess().WithNone<Base>())
        {
            if (attackable.ValueRO.currentHp > 0)
                continue;

            ecb.DestroyEntity(entity);

            if (float.IsNaN(attackable.ValueRO.currentHp) || !(attackable.ValueRO.attackableUnitType == AttackableUnitType.Boid || attackable.ValueRO.attackableUnitType == AttackableUnitType.Drone))
                continue;

            resource.ValueRW.currentRessourceCount += attackable.ValueRO.ressourceCost;
            attackable.ValueRW.ressourceCost = 0;
        }
    }
}
