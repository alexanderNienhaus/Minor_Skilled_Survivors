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
        foreach ((RefRW<PathFollow> pathFollow, RefRO<LocalTransform> localTransformEnemy, RefRW<Attacking> attacking)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRW<Attacking>>().WithAny<Alien>())
        {
            pathFollow.ValueRW.enemyPos = float3.zero;
            foreach ((RefRO<LocalTransform> localTransformUnit, RefRW<AttackableUnit> attackableUnit, Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AttackableUnit>>().WithEntityAccess())
            {
                attacking.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                if (math.length(localTransformEnemy.ValueRO.Position - localTransformUnit.ValueRO.Position) < attacking.ValueRO.range + attackableUnit.ValueRO.bounds)
                {
                    pathFollow.ValueRW.enemyPos = localTransformUnit.ValueRO.Position;
                    if (attacking.ValueRO.currentTime > attacking.ValueRO.attackSpeed)
                    {
                        attackableUnit.ValueRW.currentHp -= attacking.ValueRO.dmg;
                        attacking.ValueRW.currentTime = 0;
                        if (attackableUnit.ValueRW.currentHp <= 0)
                        {
                            ecb.DestroyEntity(entity);
                        }
                    }
                }
            }
        }
    }
}