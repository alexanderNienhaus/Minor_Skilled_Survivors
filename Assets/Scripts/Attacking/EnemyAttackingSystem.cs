using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class EnemyAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<AttackableUnit>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()  
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRW<PathFollow> pathFollow, RefRO<LocalTransform> localTransformEnemy, RefRO<Attacking> attacking)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRO<Attacking>>().WithAny<Alien>())
        {
            pathFollow.ValueRW.enemyPos = float3.zero;
            foreach ((RefRO<LocalTransform> localTransformUnit, RefRO<AttackableUnit> attackableUnit)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRO<AttackableUnit>>())
            {
                if (math.length(localTransformEnemy.ValueRO.Position - localTransformUnit.ValueRO.Position) < attacking.ValueRO.range + attackableUnit.ValueRO.bounds)
                {
                    pathFollow.ValueRW.enemyPos = localTransformUnit.ValueRO.Position;                    
                }
            }
        }
    }
}