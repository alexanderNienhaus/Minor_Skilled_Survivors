using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics;
using Unity.Collections;

[BurstCompile]
[WithAll(typeof(Tank))]
public partial struct TankAttackingJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecbParallelWriter;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<LocalTransform> allLocalTransforms;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allEntityEnemies;
    public float deltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform pLocalTransformTank, ref Attacking pAttackingTank, ref PathFollow pPathFollowTank, [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        pPathFollowTank.enemyPos = float3.zero;
        if (!pPathFollowTank.isInAttackMode)
        {
            pAttackingTank.currentTime = 0;
            return;
        }

        pLocalTransformTank.Position.y = 0;
        for (int i = 0; i < allEntityEnemies.Length; i++)
        {
            Entity enemyEntity = allEntityEnemies[i];
            LocalTransform localTransformEnemy = allLocalTransforms[enemyEntity];
            Attackable attackableEnemy = allAttackables[enemyEntity];

            float3 tankToEnemy = localTransformEnemy.Position + attackableEnemy.halfBounds - pLocalTransformTank.Position;

            float distanceTankToEnemySq = math.lengthsq(tankToEnemy);
            if (distanceTankToEnemySq - attackableEnemy.boundsRadius * attackableEnemy.boundsRadius >= pAttackingTank.range * pAttackingTank.range)
                continue;

            pPathFollowTank.enemyPos = localTransformEnemy.Position;

            pAttackingTank.currentTime += deltaTime;
            if (pAttackingTank.currentTime <= pAttackingTank.attackSpeed)
                return;

            ecbParallelWriter = SpawnProjectile(ecbParallelWriter, pLocalTransformTank, ref pAttackingTank, tankToEnemy, distanceTankToEnemySq, deltaTime, attackableEnemy.boundsRadius, pChunkIndexInQuery);
            pAttackingTank.currentTime = 0;

            allAttackables.GetRefRW(enemyEntity).ValueRW.currentHp -= pAttackingTank.dmg;

            return;
        }
    }

    [BurstCompile]
    private EntityCommandBuffer.ParallelWriter SpawnProjectile(EntityCommandBuffer.ParallelWriter pEcbParallelWriter, LocalTransform pLocalTransformTank, 
        ref Attacking pAttackingTank, float3 pTankToEnemy, float pDistanceTankToEnemySq, float pDeltaTime, float pBoundsRadius, int pChunkIndexInQuery)
    {
        Entity projectile = pEcbParallelWriter.Instantiate(pChunkIndexInQuery, pAttackingTank.projectilePrefab);

        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new LocalTransform
        {
            Position = pLocalTransformTank.Position + pAttackingTank.projectileSpawnOffset,
            Rotation = quaternion.LookRotationSafe(new float3(0, 1, 0), pTankToEnemy),
            Scale = pAttackingTank.projectileSize
        });

        //ecb.AddComponent<Parent>(projectile);
        //ecb.SetComponent(projectile, new Parent { Value = attacking.ValueRO.parent });

        float timeToLife = (pDistanceTankToEnemySq + pBoundsRadius * pBoundsRadius) / (pAttackingTank.projectileSpeed * pDeltaTime * pAttackingTank.projectileSpeed * pDeltaTime);
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });

        float3 projectileVelocity = math.normalizesafe(pTankToEnemy) * pAttackingTank.projectileSpeed * pDeltaTime;
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new PhysicsVelocity { Linear = projectileVelocity });

        return pEcbParallelWriter;
    }
}