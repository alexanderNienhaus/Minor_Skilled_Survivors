using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
[WithAll(typeof(Drone))]
public partial struct DroneAttackingJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecbParallelWriter;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<LocalTransform> allLocalTransforms;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;

    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allUnitEntities;
    public float deltaTime;

    [BurstCompile]
    public void Execute(ref LocalTransform pLocalTransformDrone, ref Attacking pAttackingDrone, ref PathFollow pPathFollowDrone, [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        pPathFollowDrone.enemyPos = float3.zero;
        //pLocalTransformDrone.Position.y = 0;
        for (int i = 0; i < allUnitEntities.Length; i++)
        {
            Entity unitEntity = allUnitEntities[i];
            LocalTransform localTransformUnit = allLocalTransforms[unitEntity];
            Attackable attackableUnit = allAttackables[unitEntity];

            if (unitEntity == null)
                return;

            float3 droneToUnit = localTransformUnit.Position + attackableUnit.halfBounds - pLocalTransformDrone.Position;

            float distanceDroneToUnitSq = math.lengthsq(droneToUnit);
            if (distanceDroneToUnitSq - attackableUnit.boundsRadius * attackableUnit.boundsRadius >= pAttackingDrone.range * pAttackingDrone.range)
                continue;

            pPathFollowDrone.enemyPos = localTransformUnit.Position;

            pAttackingDrone.currentTime += deltaTime;
            if (pAttackingDrone.currentTime <= pAttackingDrone.attackSpeed)
                return;

            ecbParallelWriter = SpawnProjectile(ecbParallelWriter, pLocalTransformDrone, ref pAttackingDrone, droneToUnit, distanceDroneToUnitSq, deltaTime, attackableUnit.boundsRadius, pChunkIndexInQuery);
            pAttackingDrone.currentTime = 0;

            allAttackables.GetRefRW(unitEntity).ValueRW.currentHp -= pAttackingDrone.dmg;
            if (attackableUnit.currentHp > 0)
                return;

            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, unitEntity);

            return;
        }
    }

    [BurstCompile]
    private EntityCommandBuffer.ParallelWriter SpawnProjectile(EntityCommandBuffer.ParallelWriter pEcbParallelWriter, LocalTransform pLocalTransformEnemy,
        ref Attacking pAttackingDrone, float3 pDroneToUnit, float pDistanceDroneToUnitSq, float pDeltaTime, float pBoundsRadius, int pChunkIndexInQuery)
    {
        Entity projectile = pEcbParallelWriter.Instantiate(pChunkIndexInQuery, pAttackingDrone.projectilePrefab);

        float3 pDroneToUnitNormalized = math.normalizesafe(pDroneToUnit);
        float3 projectileVelocity = pDroneToUnitNormalized * pAttackingDrone.projectileSpeed * pDeltaTime;
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new LocalTransform
        {
            Position = pLocalTransformEnemy.Position + pAttackingDrone.projectileSpawnOffset,
            Rotation = quaternion.LookRotation(pDroneToUnitNormalized, new float3(0, 1, 0)),
            Scale = pAttackingDrone.projectileSize
        });

        //ecb.AddComponent<Parent>(projectile);
        //ecb.SetComponent(projectile, new Parent { Value = attacking.ValueRO.parent });

        float timeToLife = (pDistanceDroneToUnitSq + pBoundsRadius * pBoundsRadius) / (pAttackingDrone.projectileSpeed * pDeltaTime * pAttackingDrone.projectileSpeed * pDeltaTime);
        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new Projectile { maxTimeToLife = timeToLife, currentTimeToLife = 0 });

        pEcbParallelWriter.SetComponent(pChunkIndexInQuery, projectile, new PhysicsVelocity { Linear = projectileVelocity });

        return pEcbParallelWriter;
    }

    /*
    private bool BufferContains(DynamicBuffer<PossibleAttackTargets> attackableUnitTypes, AttackableUnitType attackableUnitType)
    {
        for (int i = attackableUnitTypes.Length - 1; i >= 0; i--)
        {
            if (attackableUnitTypes[i].possibleAttackTarget == attackableUnitType)
                return true;
        }
        return false;
    }
    */
}