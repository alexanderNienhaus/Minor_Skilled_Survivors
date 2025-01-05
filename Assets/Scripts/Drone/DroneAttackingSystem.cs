using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial class DroneAttackingSystem : SystemBase
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
        foreach ((RefRW<PathFollow> pathFollowDrone, RefRO<LocalTransform> localTransformDrone, RefRW<Attacking> attackingDrone, DynamicBuffer<PossibleAttackTargets> possibleAttackTargets)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRW<Attacking>, DynamicBuffer<PossibleAttackTargets>>().WithAll<Drone>())
        {
            pathFollowDrone.ValueRW.enemyPos = float3.zero;
            foreach ((RefRO<LocalTransform> localTransformUnit, RefRW<Attackable> attackableUnit, Entity unit)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Attackable>>().WithEntityAccess())
            {
                if (!BufferContains(possibleAttackTargets, attackableUnit.ValueRO.attackableUnitType))
                    continue;

                float3 enemyToUnit = attackableUnit.ValueRO.halfBounds + localTransformUnit.ValueRO.Position - localTransformDrone.ValueRO.Position;
                float distanceEnemyToUnitSq = math.lengthsq(enemyToUnit);

                if (distanceEnemyToUnitSq - attackableUnit.ValueRO.boundsRadius * attackableUnit.ValueRO.boundsRadius < attackingDrone.ValueRO.range * attackingDrone.ValueRO.range)
                {
                    attackingDrone.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                    pathFollowDrone.ValueRW.enemyPos = localTransformUnit.ValueRO.Position;
                    if (attackingDrone.ValueRO.currentTime > attackingDrone.ValueRO.attackSpeed)
                    {
                        ecb = SpawnProjectile(ecb, localTransformDrone, attackingDrone, enemyToUnit, distanceEnemyToUnitSq, SystemAPI.Time.DeltaTime);

                        attackableUnit.ValueRW.currentHp -= attackingDrone.ValueRO.dmg;
                        attackingDrone.ValueRW.currentTime = 0;
                        if (attackableUnit.ValueRW.currentHp <= 0)
                        {
                            ecb.DestroyEntity(unit);
                        }
                    }
                    break;
                }
            }
        }
    }

    [BurstCompile]
    private EntityCommandBuffer SpawnProjectile(EntityCommandBuffer ecb, RefRO<LocalTransform> localTransformEnemy, RefRW<Attacking> attacking, float3 enemyToUnit, float distanceEnemyToUnit, float deltaTime)
    {
        float3 projectileVelocity = math.normalizesafe(enemyToUnit) * attacking.ValueRO.projectileSpeed * deltaTime;
        float timeToLife = 2 * (distanceEnemyToUnit / (attacking.ValueRO.projectileSpeed * deltaTime * attacking.ValueRO.projectileSpeed * deltaTime));

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