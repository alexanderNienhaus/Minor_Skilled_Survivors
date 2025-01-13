using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
[WithAll(typeof(Drone))]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct DroneAttackingJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecbParallelWriter;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allUnitEntities;
    [NativeDisableContainerSafetyRestriction] [ReadOnly] public ComponentLookup<LocalTransform> allLocalTransforms;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;
    public float deltaTime;
        
    [BurstCompile]
    public void Execute(ref LocalTransform pLocalTransformDrone, ref Attacking pAttackingDrone, ref PathFollow pPathFollowDrone, [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        pPathFollowDrone.enemyPos = float3.zero;
        for (int i = 0; i < allUnitEntities.Length; i++)
        {
            Entity unitEntity = allUnitEntities[i];
            LocalTransform localTransformUnit = allLocalTransforms[unitEntity];
            Attackable attackableUnit = allAttackables[unitEntity];

            float3 unitPos = localTransformUnit.Position + attackableUnit.halfBounds;
            float3 droneToUnit = unitPos - pLocalTransformDrone.Position;

            float distanceDroneToUnitSq = math.lengthsq(droneToUnit);
            if (distanceDroneToUnitSq - (attackableUnit.boundsRadius * attackableUnit.boundsRadius) >= (pAttackingDrone.range * pAttackingDrone.range))
                continue;

            pPathFollowDrone.enemyPos = unitPos;

            pAttackingDrone.currentTime += deltaTime;
            if (pAttackingDrone.currentTime <= pAttackingDrone.attackSpeed)
                return;

            ecbParallelWriter = SpawnProjectile(ecbParallelWriter, pLocalTransformDrone, ref pAttackingDrone, droneToUnit, distanceDroneToUnitSq, deltaTime, attackableUnit.boundsRadius, pChunkIndexInQuery);
            pAttackingDrone.currentTime = 0;

            allAttackables.GetRefRW(unitEntity).ValueRW.currentHp -= pAttackingDrone.dmg;
            if (attackableUnit.currentHp - pAttackingDrone.dmg > 0 || attackableUnit.attackableUnitType == AttackableUnitType.Base)
                continue;

            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, unitEntity);
            
            return;
        }
    }

    [BurstCompile]
    private EntityCommandBuffer.ParallelWriter SpawnProjectile(EntityCommandBuffer.ParallelWriter pEcbParallelWriter, LocalTransform pLocalTransformEnemy,
        ref Attacking pAttackingDrone, float3 pDroneToUnit, float pDistanceDroneToUnitSq, float pDeltaTime, float pBoundsRadius, int pChunkIndexInQuery)
    {
        Entity projectile = pEcbParallelWriter.Instantiate(pChunkIndexInQuery, pAttackingDrone.projectilePrefab);

        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new LocalTransform
        {
            Position = pLocalTransformEnemy.Position + pAttackingDrone.projectileSpawnOffset,
            Rotation = quaternion.LookRotationSafe(new float3(0, 1, 0), pDroneToUnit),
            Scale = pAttackingDrone.projectileSize
        });

        float timeToLife = (pDistanceDroneToUnitSq + pBoundsRadius * pBoundsRadius) / (pAttackingDrone.projectileSpeed * pDeltaTime * pAttackingDrone.projectileSpeed * pDeltaTime);
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });

        float3 projectileVelocity = math.normalizesafe(pDroneToUnit, float3.zero) * pAttackingDrone.projectileSpeed * pDeltaTime;
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new PhysicsVelocity { Linear = projectileVelocity });

        return pEcbParallelWriter;
    }
}