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
    [NativeDisableUnsafePtrRestriction] public RefRW<Resource> resource;
    public float deltaTime;

    [NativeDisableContainerSafetyRestriction] public DynamicBuffer<LinkedEntityGroup> children;

    [BurstCompile]
    public void Execute(ref LocalTransform pLocalTransformAATurret, ref AATurret pAATurret, ref Attacking pAttackingAATurret, Entity aaTurretEntity, [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        children = em.GetBuffer<LinkedEntityGroup>(aaTurretEntity);
        for (int i = 0; i < allEntityEnemies.Length; i++)
        {
            Entity enemyEntity = allEntityEnemies[i];
            LocalTransform localTransformEnemy = allLocalTransforms[enemyEntity];
            Attackable attackableEnemy = allAttackables[enemyEntity];
            PhysicsVelocity physicsVelocityEnemy = allPhysicsVelocities[enemyEntity];

            float3 aaTurretToEnemy = localTransformEnemy.Position + attackableEnemy.halfBounds - pLocalTransformAATurret.Position;

            float distanceAATurretToEnemySq = math.lengthsq(aaTurretToEnemy);
            if (distanceAATurretToEnemySq - attackableEnemy.boundsRadius * attackableEnemy.boundsRadius >= pAttackingAATurret.range * pAttackingAATurret.range)
                continue;

            pAttackingAATurret.currentTime += deltaTime;
            if (pAttackingAATurret.currentTime <= pAttackingAATurret.attackSpeed)
                return;

            ecbParallelWriter = SpawnProjectile(ecbParallelWriter, pAATurret, pAttackingAATurret, pLocalTransformAATurret.Position, localTransformEnemy.Position,
                physicsVelocityEnemy, deltaTime, pChunkIndexInQuery);
            pAttackingAATurret.currentTime = 0;

            allAttackables.GetRefRW(enemyEntity).ValueRW.currentHp -= pAttackingAATurret.dmg;
            
            if (attackableEnemy.currentHp - pAttackingAATurret.dmg > 0)
                continue;
            
            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, enemyEntity);
            resource.ValueRW.currentRessourceCount += attackableEnemy.ressourceCost;

            return;
        }
    }

    [BurstCompile]
    private EntityCommandBuffer.ParallelWriter SpawnProjectile(EntityCommandBuffer.ParallelWriter pEcbParallelWriter, AATurret pAATurret,
        Attacking pAttackingAATurret, float3 pAATurretPos, float3 pEnemyPos, PhysicsVelocity pEnemyVelocity, float pDeltaTime, int pChunkIndexInQuery)
    {
        Entity projectile = pEcbParallelWriter.Instantiate(pChunkIndexInQuery, pAttackingAATurret.projectilePrefab);

        Entity modelEntity = children.ElementAt(pAATurret.childNumberModel).Value;
        Entity mountEntity = children.ElementAt(pAATurret.childNumberMount).Value;
        Entity headEntity = children.ElementAt(pAATurret.childNumberHead).Value;

        LocalTransform modelLocalTransform = allLocalTransforms.GetRefRW(modelEntity).ValueRW;
        LocalTransform mountLocalTransform = allLocalTransforms.GetRefRW(mountEntity).ValueRW;
        LocalTransform headLocalTransform = allLocalTransforms.GetRefRW(headEntity).ValueRW;

        float3 spawnPos = pAATurretPos + modelLocalTransform.Position + mountLocalTransform.Position + headLocalTransform.Position + pAttackingAATurret.projectileSpawnOffset;
        float3 targetVelocity = pEnemyVelocity.Linear;
        float3 unitToEnemy = pEnemyPos - spawnPos;
        float unitToEnemyDist = math.length(unitToEnemy);
        float timeAdjustedProjectileSpeed = pAttackingAATurret.projectileSpeed * pDeltaTime;
        if (timeAdjustedProjectileSpeed != 0)
        {
            float projectileFlightTime = unitToEnemyDist / timeAdjustedProjectileSpeed;
            pEnemyPos += targetVelocity * projectileFlightTime;
            unitToEnemy = pEnemyPos - spawnPos;
            float timeToLife = unitToEnemyDist / timeAdjustedProjectileSpeed;
            pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });
        }

        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new LocalTransform
        {
            Position = spawnPos,
            Rotation = quaternion.LookRotationSafe(headLocalTransform.Up(), unitToEnemy),
            Scale = pAttackingAATurret.projectileSize
        });

        float3 projectileVelocity = math.normalizesafe(unitToEnemy, float3.zero) * timeAdjustedProjectileSpeed;
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new PhysicsVelocity { Linear = projectileVelocity });

        //ecb.AddComponent<Parent>(projectile);
        //ecb.SetComponent(projectile, new Parent { Value = children.ElementAt(6).Value });
        //6 10.5 13 Projectile spawn offset 0 0 4

        float3 mountToEnemy = pEnemyPos - spawnPos;
        mountToEnemy.y = 0;
        mountToEnemy = math.normalizesafe(mountToEnemy, float3.zero);
        quaternion targetRotMount = quaternion.LookRotationSafe(mountToEnemy, new float3(0, 1, 0));
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, mountEntity, new LocalTransform
        {
            Position = mountLocalTransform.Position,
            Rotation = targetRotMount,//math.slerp(mount.Rotation, targetRotMount, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = mountLocalTransform.Scale
        });

        float3 headToEnemy = pEnemyPos - spawnPos;
        headToEnemy.z = math.abs(headToEnemy.z);
        headToEnemy = math.normalizesafe(headToEnemy, float3.zero);
        quaternion targetRotHead = quaternion.LookRotationSafe(headToEnemy, new float3(0, 1, 0));
        float3 targetRotHeadEuler = ComputeAngles(targetRotHead);
        targetRotHeadEuler.y = 0;
        targetRotHead = quaternion.Euler(targetRotHeadEuler);
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, headEntity, new LocalTransform
        {
            Position = headLocalTransform.Position,
            Rotation = targetRotHead, //math.slerp(head.Rotation, targetRotHead, aaTurret.ValueRO.turnSpeed * SystemAPI.Time.DeltaTime),
            Scale = headLocalTransform.Scale
        });

        return pEcbParallelWriter;
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