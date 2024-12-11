using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;

[BurstCompile]
public partial class AATurretSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRW<AATurret> aaTurret, RefRO<LocalTransform> localTransformUnit, RefRW<Attacking> attacking, Entity unitEntity)
                    in SystemAPI.Query<RefRW<AATurret> ,RefRO<LocalTransform>, RefRW<Attacking>>().WithEntityAccess())
        {
            foreach ((RefRO<LocalTransform> localTransformEnemy, RefRW<AttackableEnemy> attackableEnemy, RefRO<Boid> boid, Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AttackableEnemy>, RefRO<Boid>>().WithEntityAccess())
            {
                float3 unitToEnemy = attackableEnemy.ValueRO.bounds * new float3(0, 1, 0)
                    + localTransformEnemy.ValueRO.Position - localTransformUnit.ValueRO.Position;
                float distanceUnitToEnemySquared = math.lengthsq(unitToEnemy);

                if (EnemyInRange(attacking, attackableEnemy, distanceUnitToEnemySquared))
                {
                    attacking.ValueRW.currentTime += SystemAPI.Time.DeltaTime;
                    if (attacking.ValueRO.currentTime > attacking.ValueRO.attackSpeed)
                    {
                        ecb = SpawnProjectile(ecb, aaTurret, attacking, localTransformUnit.ValueRO.Position,
                            localTransformEnemy.ValueRO.Position, unitEntity, boid.ValueRO);

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

    private bool EnemyInRange(RefRW<Attacking> attacking, RefRW<AttackableEnemy> attackableEnemy, float distanceUnitToEnemy)
    {
        return distanceUnitToEnemy < (attacking.ValueRO.range + attackableEnemy.ValueRO.bounds) * (attacking.ValueRO.range + attackableEnemy.ValueRO.bounds);
    }

    private EntityCommandBuffer SpawnProjectile(EntityCommandBuffer ecb, RefRW<AATurret> aaTurret, RefRW<Attacking> attacking,
        float3 unitPos, float3 enemyPos, Entity unitEntity, Boid boid)
    {
        Entity projectile = ecb.Instantiate(attacking.ValueRO.projectilePrefab);

        DynamicBuffer<LinkedEntityGroup> children = EntityManager.GetBuffer<LinkedEntityGroup>(unitEntity);
        LocalTransform mount = EntityManager.GetComponentData<LocalTransform>(children.ElementAt(2).Value);
        LocalTransform head = EntityManager.GetComponentData<LocalTransform>(children.ElementAt(5).Value);

        float3 spawnPos = unitPos + mount.Position + head.Position + attacking.ValueRO.projectileSpawnOffset;
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
        //ecb.SetComponent(projectile, new Parent { Value = attacking.ValueRO.parent });
        
        float3 mountToEnemy = enemyPos - unitPos;
        mountToEnemy.y = 0;
        mountToEnemy = math.normalize(mountToEnemy);
        quaternion targetRotMount = quaternion.LookRotation(mountToEnemy, mount.Up());
        ecb.SetComponent(children.ElementAt(2).Value, new LocalTransform
        {
            Position = mount.Position,
            Rotation = targetRotMount,//math.slerp(mount.Rotation, targetRotMount, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = mount.Scale
        });        

        float3 headToEnemy = enemyPos - spawnPos;
        headToEnemy.x = 0;
        headToEnemy.z = math.abs(headToEnemy.z);
        headToEnemy = math.normalize(headToEnemy);
        quaternion targetRotHead = quaternion.LookRotation(headToEnemy, head.Up());        
        ecb.SetComponent(children.ElementAt(5).Value, new LocalTransform  
        {
            Position = head.Position,
            Rotation = targetRotHead,//math.slerp(head.Rotation, targetRotHead, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = head.Scale
        });        
        return ecb;
    }

    /*
    float3 headWorldPos = unitPos + head.Position;
    float3 v = new float3(enemyPos.x, headWorldPos.y, enemyPos.z) - headWorldPos;
    float3 u = enemyPos - headWorldPos;

    float xAngle = math.acos(math.dot(head.Forward(), enemyPos) / (math.length(head.Forward()) * math.length(enemyPos))) * math.TODEGREES;
    //xAngle = math.angle(quaternion.Euler(head.Up()), quaternion.Euler(enemyPos)) * math.TODEGREES;
    //xAngle = anglesigned(head.Up(), enemyPos);
    //xAngle = Vector3.Angle(enemyPos, head.Up());
    Debug.Log("xAngle: "+ xAngle);
    quaternion axisAngle = quaternion.AxisAngle(new float3(1, 0, 0), (-xAngle + 90) * math.TORADIANS);
    //axisAngle = quaternion.EulerZXY(new float3(xAngle, 0, 0));
    private float Angle(float3 a, float3 b)
    {
        var v1 = a[1] - a[0];
        var v2 = a[2] - a[1];

        var cross = math.cross(v1, v2);
        var dot = math.dot(v1, v2);

        var angle = math.atan2(math.length(cross), dot);

        var test = math.dot(b, cross);
        if (test < 0.0) angle = -angle;
        return (float)angle;
    }

    private float anglesigned(float3 from, float3 to)
    {
        float angle = math.acos(math.dot(math.normalize(from), math.normalize(to)));
        float3 cross = math.cross(from, to);
        angle *= math.sign(math.dot(math.up(), cross));
        return math.degrees(angle);
    }

    [BurstCompile]
    private float GetRotationToTargetsFuturePostion(float3 pFrom, float3 pTo, float3 pTargetVelocity, float pProjectileSpeed)
    {
        if (pProjectileSpeed != 0)
        {
            float projectileFlightTime = math.length(pTo - pFrom) / pProjectileSpeed;
            pTo += pTargetVelocity * projectileFlightTime;
        }
        float angle = math.atan2(pTo.y - pFrom.y, pTo.x - pFrom.x) * math.TODEGREES - 90.0f;
        return angle;
    }
    */
}