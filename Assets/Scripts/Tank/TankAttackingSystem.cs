using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial class TankAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        foreach ((RefRW<PathFollow> pathFollowTank, RefRO<LocalTransform> localTransformTank, RefRW<Attacking> attackingTank, DynamicBuffer<PossibleAttackTargets> possibleAttackTargets)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRW<Attacking>, DynamicBuffer<PossibleAttackTargets>>().WithAny<Tank>())
        {
            pathFollowTank.ValueRW.enemyPos = float3.zero;
            if (!pathFollowTank.ValueRO.isInAttackMode)
            {
                attackingTank.ValueRW.currentTime = 0;
                continue;
            }

            foreach ((RefRO<LocalTransform> localTransformEnemy, RefRW<Attackable> attackableEnemy, Entity entityEnemy)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Attackable>>().WithEntityAccess().WithAll<Drone>())
            {
                float3 unitToEnemy = attackableEnemy.ValueRO.halfBounds + localTransformEnemy.ValueRO.Position - localTransformTank.ValueRO.Position;
                float distanceUnitToEnemySq = math.lengthsq(unitToEnemy);

                if (distanceUnitToEnemySq - attackableEnemy.ValueRO.boundsRadius * attackableEnemy.ValueRO.boundsRadius < attackingTank.ValueRO.range * attackingTank.ValueRO.range)
                {
                    attackingTank.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                    pathFollowTank.ValueRW.enemyPos = localTransformEnemy.ValueRO.Position;
                    if (attackingTank.ValueRO.currentTime > attackingTank.ValueRO.attackSpeed)
                    {
                        ecb = SpawnProjectile(ecb, localTransformTank, attackingTank, unitToEnemy, distanceUnitToEnemySq);

                        attackableEnemy.ValueRW.currentHp -= attackingTank.ValueRO.dmg;
                        attackingTank.ValueRW.currentTime = 0;
                        if (attackableEnemy.ValueRW.currentHp <= 0)
                        {
                            EventBus<OnResourceChangedEvent>.Publish(new OnResourceChangedEvent(attackableEnemy.ValueRO.ressourceCost));
                            ecb.DestroyEntity(entityEnemy);
                        }
                    }
                    break;
                }
            }
        }
    }

    private EntityCommandBuffer SpawnProjectile(EntityCommandBuffer ecb, RefRO<LocalTransform> localTransformEnemy, RefRW<Attacking> attacking, float3 enemyToUnit, float distanceEnemyToUnit)
    {
        float3 projectileVelocity = math.normalizesafe(enemyToUnit) * attacking.ValueRO.projectileSpeed * SystemAPI.Time.DeltaTime;
        float timeToLife = 2 * (distanceEnemyToUnit / (attacking.ValueRO.projectileSpeed * attacking.ValueRO.projectileSpeed * SystemAPI.Time.DeltaTime * SystemAPI.Time.DeltaTime));

        Entity projectile = ecb.Instantiate(attacking.ValueRO.projectilePrefab);
        ecb.SetComponent(projectile, new LocalTransform
        {
            Position = localTransformEnemy.ValueRO.Position + attacking.ValueRO.projectileSpawnOffset,
            Rotation = quaternion.Euler(enemyToUnit),
            Scale = 0.5f
        });
        //ecb.AddComponent<Parent>(projectile);
        //ecb.SetComponent(projectile, new Parent { Value = attacking.ValueRO.parent });
        ecb.SetComponent(projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });
        ecb.SetComponent(projectile, new PhysicsVelocity { Linear = projectileVelocity });
        return ecb;
    }

    private bool BufferContains(DynamicBuffer<PossibleAttackTargets> attackableUnitTypes, AttackableUnitType attackableUnitType)
    {
        for (int i = attackableUnitTypes.Length - 1; i >= 0; i--)
        {
            if (attackableUnitTypes[i].possibleAttackTarget == attackableUnitType)
                return true;
        }
        return false;
    }
}