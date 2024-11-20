using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
        foreach ((RefRW<PathFollow> pathFollow, RefRO<LocalTransform> localTransformUnit, RefRW<Attacking> attacking)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRW<Attacking>>().WithAny<Soldier>())
        {
            pathFollow.ValueRW.enemyPos = float3.zero;
            if (!pathFollow.ValueRO.isInAttackMode)
            {
                continue;
            }

            foreach ((RefRO<LocalTransform> localTransformEnemy, RefRW<AttackableEnemy> attackableEnemy, Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AttackableEnemy>>().WithEntityAccess())
            {
                attacking.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                if (math.length(localTransformUnit.ValueRO.Position - localTransformEnemy.ValueRO.Position) < attacking.ValueRO.range + attackableEnemy.ValueRO.bounds)
                {
                    pathFollow.ValueRW.enemyPos = localTransformEnemy.ValueRO.Position;
                    if (attacking.ValueRO.currentTime > attacking.ValueRO.attackSpeed)
                    {
                        attackableEnemy.ValueRW.currentHp -= attacking.ValueRO.dmg;
                        attacking.ValueRW.currentTime = 0;
                        if (attackableEnemy.ValueRW.currentHp <= 0)
                        {
                            ecb.DestroyEntity(entity);
                        }
                    }
                }
            }
        }
    }
}