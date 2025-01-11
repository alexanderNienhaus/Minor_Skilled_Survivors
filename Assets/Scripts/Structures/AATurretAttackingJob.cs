using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
[WithAll(typeof(AATurret))]
public partial struct AATurretAttackingJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public EntityManager em;
    public EntityCommandBuffer.ParallelWriter ecbParallelWriter;

    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allEntityEnemies;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;
    [NativeDisableContainerSafetyRestriction] [ReadOnly] public ComponentLookup<LocalTransform> allLocalTransforms;
    [ReadOnly] public ComponentLookup<PhysicsVelocity> allPhysicsVelocities;
    [ReadOnly] public CollisionWorld collisionWorld;
    [NativeDisableUnsafePtrRestriction] public RefRW<Resource> resource;
    public float deltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform pLocalTransformAATurret, ref AATurret pAATurret, ref Attacking pAttackingAATurret, Entity aaTurretEntity, [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        Attack(pLocalTransformAATurret, ref pAATurret, ref pAttackingAATurret, aaTurretEntity, pChunkIndexInQuery);
    }

    [BurstCompile]
    private void Attack(LocalTransform pLocalTransformAATurret, ref AATurret pAATurret, ref Attacking pAttackingAATurret, Entity aaTurretEntity, int pChunkIndexInQuery)
    {
        Entity mountEntity, headEntity;
        LocalTransform mountLocalTransform, headLocalTransform;
        float3 spawnPos, spawnToEnemy;
        float spawnToEnemyDist, timeAdjustedProjectileSpeed;

        pAttackingAATurret.currentTime += deltaTime;
        if (pAttackingAATurret.currentTime <= pAttackingAATurret.attackSpeed)
            return;

        if ((allLocalTransforms.HasComponent(pAATurret.target) && CheckSpawnToEnemyForObstacle(pLocalTransformAATurret, pAATurret, pAttackingAATurret, aaTurretEntity,
            allLocalTransforms[pAATurret.target], allPhysicsVelocities[pAATurret.target], out mountEntity, out headEntity, out mountLocalTransform, out headLocalTransform,
                out spawnPos, out spawnToEnemy, out spawnToEnemyDist, out timeAdjustedProjectileSpeed))

                || FindTarget(ref pAATurret, aaTurretEntity, pLocalTransformAATurret, pAttackingAATurret, out mountEntity, out headEntity, out mountLocalTransform, out headLocalTransform,
                out spawnPos, out spawnToEnemy, out spawnToEnemyDist, out timeAdjustedProjectileSpeed))
        {
            Entity nearestEnemyEntity = pAATurret.target;
            LocalTransform nearestLocaltransformEnemy = allLocalTransforms[nearestEnemyEntity];
            Attackable nearestAttackableEnemy = allAttackables[nearestEnemyEntity];
            PhysicsVelocity nearestPhysicsVelocityEnemy = allPhysicsVelocities[nearestEnemyEntity];   

            ecbParallelWriter = SpawnProjectile(ecbParallelWriter, pAttackingAATurret, nearestPhysicsVelocityEnemy, spawnPos, nearestLocaltransformEnemy.Position, spawnToEnemyDist,
                    timeAdjustedProjectileSpeed, spawnToEnemy, mountEntity, headEntity, mountLocalTransform, headLocalTransform, deltaTime, pChunkIndexInQuery);
            pAttackingAATurret.currentTime = 0;

            allAttackables.GetRefRW(nearestEnemyEntity).ValueRW.currentHp -= pAttackingAATurret.dmg;

            if (nearestAttackableEnemy.currentHp - pAttackingAATurret.dmg > 0)
                return;

            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, nearestEnemyEntity);
            resource.ValueRW.currentRessourceCount += nearestAttackableEnemy.ressourceCost;
            return;
        }
    }

    [BurstCompile]
    private bool CheckSpawnToEnemyForObstacle(LocalTransform pLocalTransformAATurret, AATurret pAATurret, Attacking pAttackingAATurret, Entity aaTurretEntity, LocalTransform nearestLocaltransformEnemy, PhysicsVelocity nearestPhysicsVelocityEnemy, out Entity mountEntity, out Entity headEntity, out LocalTransform mountLocalTransform, out LocalTransform headLocalTransform, out float3 spawnPos, out float3 spawnToEnemy, out float spawnToEnemyDist, out float timeAdjustedProjectileSpeed)
    {
        DynamicBuffer<LinkedEntityGroup> children = em.GetBuffer<LinkedEntityGroup>(aaTurretEntity);
        Entity modelEntity = children.ElementAt(pAATurret.childNumberModel).Value;
        mountEntity = children.ElementAt(pAATurret.childNumberMount).Value;
        headEntity = children.ElementAt(pAATurret.childNumberHead).Value;
        LocalTransform modelLocalTransform = allLocalTransforms.GetRefRW(modelEntity).ValueRW;
        mountLocalTransform = allLocalTransforms.GetRefRW(mountEntity).ValueRW;
        headLocalTransform = allLocalTransforms.GetRefRW(headEntity).ValueRW;
        spawnPos = pLocalTransformAATurret.Position + modelLocalTransform.Position + mountLocalTransform.Position + headLocalTransform.Position + pAttackingAATurret.projectileSpawnOffset;
        float3 enemyPos = nearestLocaltransformEnemy.Position;
        float3 targetVelocity = nearestPhysicsVelocityEnemy.Linear;
        spawnToEnemy = enemyPos - spawnPos;
        spawnToEnemyDist = math.length(spawnToEnemy);
        timeAdjustedProjectileSpeed = pAttackingAATurret.projectileSpeed * deltaTime;
        if (timeAdjustedProjectileSpeed != 0)
        {
            float projectileFlightTime = spawnToEnemyDist / timeAdjustedProjectileSpeed;
            enemyPos += targetVelocity * projectileFlightTime;
            spawnToEnemy = enemyPos - spawnPos;
        }

        if (Raycast(spawnPos, enemyPos, out RaycastHit rayCastHit))
            return false;

        return true;
    }

    [BurstCompile]
    private bool FindTarget(ref AATurret pAATurret, Entity pAATurretEntity, LocalTransform pLocalTransformAATurret, Attacking pAttackingAATurret,
        out Entity pMountEntity, out Entity pHeadEntity, out LocalTransform pMountLocalTransform, out LocalTransform pHeadLocalTransform,
        out float3 pSpawnPos, out float3 pSpawnToEnemy, out float pSpawnToEnemyDist, out float pTimeAdjustedProjectileSpeed)
    {
        pMountEntity = Entity.Null;
        pHeadEntity = Entity.Null;
        pMountLocalTransform = LocalTransform.Identity;
        pHeadLocalTransform = LocalTransform.Identity;
        pSpawnPos = float3.zero;
        pSpawnToEnemy = float3.zero;
        pSpawnToEnemyDist = 0;
        pTimeAdjustedProjectileSpeed = 0;

        float shortestDistanceSq = float.MaxValue;
        Entity nearestEntity = Entity.Null;
        for (int i = 0; i < allEntityEnemies.Length; i++)
        {
            Entity enemyEntity = allEntityEnemies[i];
            LocalTransform localTransformEnemy = allLocalTransforms[enemyEntity];
            Attackable attackableEnemy = allAttackables[enemyEntity];

            float3 aaTurretToEnemy = localTransformEnemy.Position + attackableEnemy.halfBounds - pLocalTransformAATurret.Position;

            float distanceAATurretToEnemySq = math.lengthsq(aaTurretToEnemy);
            if (distanceAATurretToEnemySq - attackableEnemy.boundsRadius * attackableEnemy.boundsRadius >= pAttackingAATurret.range * pAttackingAATurret.range)
                continue;

            if(CheckSpawnToEnemyForObstacle(pLocalTransformAATurret, pAATurret, pAttackingAATurret, pAATurretEntity, allLocalTransforms[enemyEntity],
                allPhysicsVelocities[enemyEntity], out pMountEntity, out pHeadEntity, out pMountLocalTransform, out pHeadLocalTransform,
                out pSpawnPos, out pSpawnToEnemy, out pSpawnToEnemyDist, out pTimeAdjustedProjectileSpeed))

            shortestDistanceSq = distanceAATurretToEnemySq;
            nearestEntity = enemyEntity;
        }

        if (shortestDistanceSq < float.MaxValue)
        {
            pAATurret.target = nearestEntity;
            return true;
        }

        return false;
    }

    [BurstCompile]
    private EntityCommandBuffer.ParallelWriter SpawnProjectile(EntityCommandBuffer.ParallelWriter pEcbParallelWriter, Attacking pAttackingAATurret, PhysicsVelocity pEnemyVelocity,
        float3 pSpawnPos, float3 pEnemyPos, float pUnitToEnemyDist, float pTimeAdjustedProjectileSpeed, float3 pSpawnToEnemy,
        Entity pMountEntity, Entity pHeadEntity, LocalTransform pMountLocaltransform, LocalTransform pHeadLocaltransform, float pDeltaTime, int pChunkIndexInQuery)
    {
        Entity projectile = pEcbParallelWriter.Instantiate(pChunkIndexInQuery, pAttackingAATurret.projectilePrefab);

        float timeToLife = pUnitToEnemyDist / pTimeAdjustedProjectileSpeed;
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });

        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new LocalTransform
        {
            Position = pSpawnPos,
            Rotation = quaternion.LookRotationSafe(pHeadLocaltransform.Up(), pSpawnToEnemy),
            Scale = pAttackingAATurret.projectileSize
        });

        float3 projectileVelocity = math.normalizesafe(pSpawnToEnemy, float3.zero) * pTimeAdjustedProjectileSpeed;
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new PhysicsVelocity { Linear = projectileVelocity });

        //ecb.AddComponent<Parent>(projectile);
        //ecb.SetComponent(projectile, new Parent { Value = children.ElementAt(6).Value });
        //6 10.5 13 Projectile spawn offset 0 0 4

        float3 mountToEnemy = pEnemyPos - pSpawnPos;
        mountToEnemy.y = 0;
        mountToEnemy = math.normalizesafe(mountToEnemy, float3.zero);
        quaternion targetRotMount = quaternion.LookRotationSafe(mountToEnemy, new float3(0, 1, 0));
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, pMountEntity, new LocalTransform
        {
            Position = pMountLocaltransform.Position,
            Rotation = targetRotMount,//math.slerp(mount.Rotation, targetRotMount, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = pMountLocaltransform.Scale
        });

        float3 headToEnemy = pEnemyPos - pSpawnPos;
        headToEnemy.z = math.abs(headToEnemy.z);
        headToEnemy = math.normalizesafe(headToEnemy, float3.zero);
        quaternion targetRotHead = quaternion.LookRotationSafe(headToEnemy, new float3(0, 1, 0));
        float3 targetRotHeadEuler = ComputeAngles(targetRotHead);
        targetRotHeadEuler.y = 0;
        targetRotHead = quaternion.Euler(targetRotHeadEuler);
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, pHeadEntity, new LocalTransform
        {
            Position = pHeadLocaltransform.Position,
            Rotation = targetRotHead, //math.slerp(head.Rotation, targetRotHead, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = pHeadLocaltransform.Scale
        });

        return pEcbParallelWriter;
    }

    [BurstCompile]
    private bool Raycast(float3 pRayStart, float3 pRayEnd, out RaycastHit pRaycastHit)
    {
        RaycastInput raycastInput = new RaycastInput
        {
            Start = pRayStart,
            End = pRayEnd,
            Filter = new CollisionFilter
            {
                BelongsTo = (uint)CollisionLayers.AATurret,
                CollidesWith = (uint)(CollisionLayers.Building)
            }
        };
        return collisionWorld.CastRay(raycastInput, out pRaycastHit);
    }

    [BurstCompile]
    private float ComputeXAngle(quaternion q)
    {
        float sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
        float cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
        return math.atan2(sinr_cosp, cosr_cosp);
    }

    [BurstCompile]
    private float ComputeYAngle(quaternion q)
    {
        float sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
        if (math.abs(sinp) >= 1)
            return math.PI / 2 * math.sign(sinp); // use 90 degrees if out of range
        else
            return math.asin(sinp);
    }

    [BurstCompile]
    private float ComputeZAngle(quaternion q)
    {
        float siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
        float cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
        return math.atan2(siny_cosp, cosy_cosp);
    }

    [BurstCompile]
    private float3 ComputeAngles(quaternion q)
    {
        return new float3(ComputeXAngle(q), ComputeYAngle(q), ComputeZAngle(q));
    }

    [BurstCompile]
    private quaternion FromAngles(float3 angles)
    {

        float cy = math.cos(angles.z * 0.5f);
        float sy = math.sin(angles.z * 0.5f);
        float cp = math.cos(angles.y * 0.5f);
        float sp = math.sin(angles.y * 0.5f);
        float cr = math.cos(angles.x * 0.5f);
        float sr = math.sin(angles.x * 0.5f);

        float4 q;
        q.w = cr * cp * cy + sr * sp * sy;
        q.x = sr * cp * cy - cr * sp * sy;
        q.y = cr * sp * cy + sr * cp * sy;
        q.z = cr * cp * sy - sr * sp * cy;

        return q;
    }
}