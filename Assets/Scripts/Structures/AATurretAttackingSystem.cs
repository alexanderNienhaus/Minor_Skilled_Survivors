using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;

[BurstCompile]
public partial class AATurretAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        foreach ((RefRW<AATurret> aaTurret, RefRO<LocalTransform> localTransformAATurret, RefRW<Attacking> attackingAATurret, DynamicBuffer<PossibleAttackTargets> possibleAttackTargets, Entity entityAATurret)
                    in SystemAPI.Query<RefRW<AATurret>, RefRO<LocalTransform>, RefRW<Attacking>, DynamicBuffer<PossibleAttackTargets>>().WithEntityAccess())
        {
            foreach ((RefRO<LocalTransform> localTransformEnemy, RefRW<Attackable> attackableEnemy, RefRO<Boid> boid, Entity entityEnemy)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Attackable>, RefRO<Boid>>().WithEntityAccess().WithAll<Boid>())
            {
                float3 unitToEnemy = attackableEnemy.ValueRO.halfBounds + localTransformEnemy.ValueRO.Position - localTransformAATurret.ValueRO.Position;
                float distanceUnitToEnemySq = math.lengthsq(unitToEnemy);

                if (distanceUnitToEnemySq - attackableEnemy.ValueRO.boundsRadius * attackableEnemy.ValueRO.boundsRadius < attackingAATurret.ValueRO.range * attackingAATurret.ValueRO.range)
                {
                    attackingAATurret.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                    if (attackingAATurret.ValueRO.currentTime > attackingAATurret.ValueRO.attackSpeed)
                    {
                        ecb = SpawnProjectile(ecb, aaTurret, attackingAATurret, localTransformAATurret.ValueRO.Position,
                            localTransformEnemy.ValueRO.Position, entityAATurret, boid.ValueRO);

                        attackableEnemy.ValueRW.currentHp -= attackingAATurret.ValueRO.dmg;
                        attackingAATurret.ValueRW.currentTime = 0;
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

    private EntityCommandBuffer SpawnProjectile(EntityCommandBuffer ecb, RefRW<AATurret> aaTurret, RefRW<Attacking> attacking,
        float3 unitPos, float3 enemyPos, Entity unitEntity, Boid boid)
    {
        Entity projectile = ecb.Instantiate(attacking.ValueRO.projectilePrefab);

        DynamicBuffer<LinkedEntityGroup> children = EntityManager.GetBuffer<LinkedEntityGroup>(unitEntity);
        LocalTransform mount = EntityManager.GetComponentData<LocalTransform>(children.ElementAt(3).Value);
        LocalTransform head = EntityManager.GetComponentData<LocalTransform>(children.ElementAt(6).Value);
        LocalTransform model = EntityManager.GetComponentData<LocalTransform>(children.ElementAt(1).Value);

        float3 spawnPos = unitPos + model.Position + mount.Position + head.Position + attacking.ValueRO.projectileSpawnOffset;
        float3 targetVelocity = boid.velocity;
        float3 unitToEnemy = enemyPos - spawnPos;
        float unitToEnemyDist = math.length(unitToEnemy);
        float timeAdjustedProjectileSpeed = attacking.ValueRO.projectileSpeed * SystemAPI.Time.DeltaTime;

        if (timeAdjustedProjectileSpeed != 0)
        {
            float projectileFlightTime = unitToEnemyDist / timeAdjustedProjectileSpeed;
            enemyPos += targetVelocity * projectileFlightTime;
            unitToEnemy = enemyPos - spawnPos;
            float timeToLife = unitToEnemyDist / timeAdjustedProjectileSpeed;
            ecb.SetComponent(projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });
        }

        float3 projectileVelocity = math.normalizesafe(unitToEnemy) * timeAdjustedProjectileSpeed;
        ecb.SetComponent(projectile, new PhysicsVelocity { Linear = projectileVelocity });

        ecb.SetComponent(projectile, new LocalTransform
        {
            Position = spawnPos,
            Rotation = quaternion.Euler(unitToEnemy),
            Scale = 1f
        });

        //ecb.AddComponent<Parent>(projectile);
        //ecb.SetComponent(projectile, new Parent { Value = children.ElementAt(6).Value });

        //6 10.5 13 Projectile spawn offset 0 0 4

        float3 headToEnemy = enemyPos - spawnPos;
        headToEnemy.x = 0;
        headToEnemy.z = math.abs(headToEnemy.z);
        headToEnemy = math.normalize(headToEnemy);
        quaternion targetRotHead = quaternion.LookRotation(headToEnemy, head.Up());        
        ecb.SetComponent(children.ElementAt(6).Value, new LocalTransform
        {
            Position = head.Position,
            Rotation = targetRotHead,//math.slerp(head.Rotation, targetRotHead, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = head.Scale
        });
        
        float3 mountToEnemy = enemyPos - spawnPos;
        mountToEnemy.y = 0;
        mountToEnemy = math.normalize(mountToEnemy);
        quaternion targetRotMount = quaternion.LookRotation(mountToEnemy, mount.Up());
        ecb.SetComponent(children.ElementAt(3).Value, new LocalTransform
        {
            Position = mount.Position,
            Rotation = targetRotMount,//math.slerp(mount.Rotation, targetRotMount, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = mount.Scale
        });   
        
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