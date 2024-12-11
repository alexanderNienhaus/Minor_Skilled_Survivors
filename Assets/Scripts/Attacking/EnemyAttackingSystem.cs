using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial class EnemyAttackingSystem : SystemBase
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
        foreach ((RefRW<PathFollow> pathFollow, RefRO<LocalTransform> localTransformEnemy, RefRW<Attacking> attacking)
            in SystemAPI.Query<RefRW<PathFollow>, RefRO<LocalTransform>, RefRW<Attacking>>().WithAny<Drone>())
        {
            pathFollow.ValueRW.enemyPos = float3.zero;
            foreach ((RefRO<LocalTransform> localTransformUnit, RefRW<AttackableUnit> attackableUnit, Entity unit)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AttackableUnit>>().WithEntityAccess())
            {
                float3 enemyToUnit = attackableUnit.ValueRO.bounds * new float3(0, 1, 0) + localTransformUnit.ValueRO.Position - localTransformEnemy.ValueRO.Position;
                float distanceEnemyToUnit = math.lengthsq(enemyToUnit);
                if (distanceEnemyToUnit < (attacking.ValueRO.range + attackableUnit.ValueRO.bounds) * (attacking.ValueRO.range + attackableUnit.ValueRO.bounds))
                {
                    attacking.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                    pathFollow.ValueRW.enemyPos = localTransformUnit.ValueRO.Position;
                    if (attacking.ValueRO.currentTime > attacking.ValueRO.attackSpeed)
                    {
                        ecb = SpawnProjectile(ecb, localTransformEnemy, attacking, enemyToUnit, distanceEnemyToUnit, SystemAPI.Time.DeltaTime);

                        attackableUnit.ValueRW.currentHp -= attacking.ValueRO.dmg;
                        attacking.ValueRW.currentTime = 0;
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
        float timeToLife = distanceEnemyToUnit / (attacking.ValueRO.projectileSpeed * deltaTime);

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