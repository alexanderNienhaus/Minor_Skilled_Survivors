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

    [NativeDisableContainerSafetyRestriction] public DynamicBuffer<LinkedEntityGroup> children;

    [BurstCompile]
    public void Execute(ref LocalTransform pLocalTransformAATurret, ref AATurret pAATurret, ref Attacking pAttackingAATurret, Entity pAATurretEntity,
        [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        pAttackingAATurret.currentTime += deltaTime;
        if (pAttackingAATurret.currentTime <= pAttackingAATurret.attackSpeed)
            return;

        children = em.GetBuffer<LinkedEntityGroup>(pAATurretEntity);
        for (int i = 0; i < allEntityEnemies.Length; i++)
        {
            Entity enemyEntity = allEntityEnemies[i];
            LocalTransform localTransformEnemy = allLocalTransforms[enemyEntity];
            Attackable attackableEnemy = allAttackables[enemyEntity];
            PhysicsVelocity physicsVelocityEnemy = allPhysicsVelocities[enemyEntity];

            float3 enemyPos = localTransformEnemy.Position + attackableEnemy.halfBounds;
            float3 aaTurretToEnemy = enemyPos - pLocalTransformAATurret.Position;
            float distanceAATurretToEnemySq = math.lengthsq(aaTurretToEnemy);
            if (distanceAATurretToEnemySq - attackableEnemy.boundsRadius * attackableEnemy.boundsRadius >= pAttackingAATurret.range * pAttackingAATurret.range)
                continue;

            Entity modelEntity = children.ElementAt(pAATurret.childNumberModel).Value;
            Entity mountEntity = children.ElementAt(pAATurret.childNumberMount).Value;
            Entity headEntity = children.ElementAt(pAATurret.childNumberHead).Value;
            LocalTransform modelLocalTransform = allLocalTransforms.GetRefRW(modelEntity).ValueRW;
            LocalTransform mountLocalTransform = allLocalTransforms.GetRefRW(mountEntity).ValueRW;
            LocalTransform headLocalTransform = allLocalTransforms.GetRefRW(headEntity).ValueRW;
            float3 spawnPos = pLocalTransformAATurret.Position + modelLocalTransform.Position + mountLocalTransform.Position + headLocalTransform.Position + pAttackingAATurret.projectileSpawnOffset;
            if (Raycast(spawnPos, enemyPos, out _))
                continue;

            pAttackingAATurret.currentTime = 0;
            ecbParallelWriter = SpawnProjectile(ecbParallelWriter, pAttackingAATurret, enemyPos, physicsVelocityEnemy, spawnPos, mountLocalTransform, headLocalTransform,
                mountEntity, headEntity, deltaTime, pChunkIndexInQuery);

            allAttackables.GetRefRW(enemyEntity).ValueRW.currentHp -= pAttackingAATurret.dmg;            
            if (attackableEnemy.currentHp - pAttackingAATurret.dmg > 0)
                continue;
            
            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, enemyEntity);
            resource.ValueRW.currentRessourceCount += attackableEnemy.ressourceCost;

            return;
        }
    }

    [BurstCompile]
    private bool Raycast(float3 pRayStart, float3 pRayEnd, out RaycastHit pRaycastHit)
    {
        RaycastInput raycastInput = new ()
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
    private EntityCommandBuffer.ParallelWriter SpawnProjectile(EntityCommandBuffer.ParallelWriter pEcbParallelWriter, Attacking pAttackingAATurret, float3 pEnemyPos,
        PhysicsVelocity pEnemyVelocity, float3 pSpawnPos, LocalTransform pMountLocalTransform, LocalTransform pHeadLocalTransform, Entity pMountEntity, Entity pHeadEntity,
        float pDeltaTime, int pChunkIndexInQuery)
    {
        Entity projectile = pEcbParallelWriter.Instantiate(pChunkIndexInQuery, pAttackingAATurret.projectilePrefab);

        float3 targetVelocity = pEnemyVelocity.Linear;
        float3 unitToEnemy = pEnemyPos - pSpawnPos;
        float unitToEnemyDist = math.length(unitToEnemy);
        float timeAdjustedProjectileSpeed = pAttackingAATurret.projectileSpeed * pDeltaTime;
        if (timeAdjustedProjectileSpeed != 0)
        {
            float projectileFlightTime = unitToEnemyDist / timeAdjustedProjectileSpeed;
            pEnemyPos += targetVelocity * projectileFlightTime;
            unitToEnemy = pEnemyPos - pSpawnPos;
            float timeToLife = unitToEnemyDist / timeAdjustedProjectileSpeed;
            pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });
        }

        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new LocalTransform
        {
            Position = pSpawnPos,
            Rotation = quaternion.LookRotationSafe(pHeadLocalTransform.Up(), unitToEnemy),
            Scale = pAttackingAATurret.projectileSize
        });

        float3 projectileVelocity = math.normalizesafe(unitToEnemy, float3.zero) * timeAdjustedProjectileSpeed;
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
            Position = pMountLocalTransform.Position,
            Rotation = targetRotMount,//math.slerp(mount.Rotation, targetRotMount, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = pMountLocalTransform.Scale
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
            Position = pHeadLocalTransform.Position,
            Rotation = targetRotHead, //math.slerp(head.Rotation, targetRotHead, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = pHeadLocalTransform.Scale
        });

        return pEcbParallelWriter;
    }

    [BurstCompile]
    private float ComputeXAngle(quaternion pQ)
    {
        float sinr_cosp = 2 * (pQ.value.w * pQ.value.x + pQ.value.y * pQ.value.z);
        float cosr_cosp = 1 - 2 * (pQ.value.x * pQ.value.x + pQ.value.y * pQ.value.y);
        return math.atan2(sinr_cosp, cosr_cosp);
    }

    [BurstCompile]
    private float ComputeYAngle(quaternion pQ)
    {
        float sinp = 2 * (pQ.value.w * pQ.value.y - pQ.value.z * pQ.value.x);
        if (math.abs(sinp) >= 1)
            return math.PI / 2 * math.sign(sinp); // use 90 degrees if out of range
        else
            return math.asin(sinp);
    }

    [BurstCompile]
    private float ComputeZAngle(quaternion pQ)
    {
        float siny_cosp = 2 * (pQ.value.w * pQ.value.z + pQ.value.x * pQ.value.y);
        float cosy_cosp = 1 - 2 * (pQ.value.y * pQ.value.y + pQ.value.z * pQ.value.z);
        return math.atan2(siny_cosp, cosy_cosp);
    }

    [BurstCompile]
    private float3 ComputeAngles(quaternion pQ)
    {
        return new float3(ComputeXAngle(pQ), ComputeYAngle(pQ), ComputeZAngle(pQ));
    }

    [BurstCompile]
    private quaternion FromAngles(float3 pAngles)
    {

        float cy = math.cos(pAngles.z * 0.5f);
        float sy = math.sin(pAngles.z * 0.5f);
        float cp = math.cos(pAngles.y * 0.5f);
        float sp = math.sin(pAngles.y * 0.5f);
        float cr = math.cos(pAngles.x * 0.5f);
        float sr = math.sin(pAngles.x * 0.5f);

        float4 q;
        q.w = cr * cp * cy + sr * sp * sy;
        q.x = sr * cp * cy - cr * sp * sy;
        q.y = cr * sp * cy + sr * cp * sy;
        q.z = cr * cp * sy - sr * sp * cy;

        return q;
    }
}