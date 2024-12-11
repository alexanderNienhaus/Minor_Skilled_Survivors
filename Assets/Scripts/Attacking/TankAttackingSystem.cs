using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial class TankAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRW<PathFollow> pathFollow, RefRO<LocalTransform> localTransformTank, RefRW<Attacking> attacking)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRW<Attacking>>().WithAny<Tank>())
        {
            pathFollow.ValueRW.enemyPos = float3.zero;
            if (!pathFollow.ValueRO.isInAttackMode)
            {
                attacking.ValueRW.currentTime = 0;
                continue;
            }

            foreach ((RefRO<LocalTransform> localTransformEnemy, RefRW<AttackableEnemy> attackableEnemy, Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AttackableEnemy>>().WithEntityAccess().WithAll<Drone>())
            {
                float3 unitToEnemy = attackableEnemy.ValueRO.bounds * new float3(0, 1, 0) + localTransformEnemy.ValueRO.Position - localTransformTank.ValueRO.Position;
                float distanceUnitToEnemy = math.lengthsq(unitToEnemy);
                if (distanceUnitToEnemy < (attacking.ValueRO.range + attackableEnemy.ValueRO.bounds) * (attacking.ValueRO.range + attackableEnemy.ValueRO.bounds))
                {
                    attacking.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                    pathFollow.ValueRW.enemyPos = localTransformEnemy.ValueRO.Position;
                    if (attacking.ValueRO.currentTime > attacking.ValueRO.attackSpeed)
                    {
                        ecb = SpawnProjectile(ecb, localTransformTank, attacking, unitToEnemy, distanceUnitToEnemy);

                        attackableEnemy.ValueRW.currentHp -= attacking.ValueRO.dmg;
                        attacking.ValueRW.currentTime = 0;
                        if (attackableEnemy.ValueRW.currentHp <= 0)
                        {
                            ecb.DestroyEntity(entity);
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
        float timeToLife = distanceEnemyToUnit / (attacking.ValueRO.projectileSpeed * SystemAPI.Time.DeltaTime);

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
}