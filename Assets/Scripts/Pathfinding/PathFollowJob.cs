using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics;

[BurstCompile]
public partial struct PathFollowJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public EntityManager em;
    [NativeDisableContainerSafetyRestriction] public BufferLookup<PathPositions> allPathPositions;
    public float3 gridOriginPos;
    public float gridCellSize;
    public float deltaTime;

    [BurstCompile]
    public void Execute(Entity pEntity, ref LocalTransform pLocalTransform, ref PhysicsVelocity pPhysicsVelocity, ref PathFollow pathFollow)
    {
        if (!em.Exists(pEntity) || !allPathPositions.HasBuffer(pEntity))
            return;

        if (float.IsNaN(pathFollow.enemyPos.x) || float.IsNaN(pathFollow.enemyPos.y) || float.IsNaN(pathFollow.enemyPos.z))
        {
            pathFollow.enemyPos = float3.zero;
        }

        if (!math.all(pathFollow.enemyPos == float3.zero))
        {
            pPhysicsVelocity.Linear = float3.zero;
            quaternion targetRotation = quaternion.LookRotationSafe(pathFollow.enemyPos - pLocalTransform.Position, new float3(0, 1, 0));
            pLocalTransform.Rotation = math.slerp(pLocalTransform.Rotation, targetRotation, pathFollow.rotationSpeed * deltaTime);
            return;
        }

        DynamicBuffer<PathPositions> pathPositions = allPathPositions[pEntity];
        int length = pathPositions.Length;

        if (length < 2)
            return;

        float3 lastPos = pathPositions[length - 1].pos;
        float3 currentPos = pLocalTransform.Position;
        float3 nextPos = pathPositions[length - 2].pos;
        float3 moveVec = nextPos - currentPos;
        moveVec.y = 0;

        float3 moveDir = math.normalizesafe(moveVec, float3.zero);
        pPhysicsVelocity.Linear = moveDir * pathFollow.movementSpeed * deltaTime;

        if (length > 2 && PassedNode(new float2(lastPos.x, lastPos.z), new float2(nextPos.x, nextPos.z), new float2(currentPos.x, currentPos.z)))
        {
            //Reached next waypoint
            pathPositions.RemoveAt(length - 1);
        }
        else if (length == 2 && math.distance(currentPos, nextPos) < pathFollow.checkDistanceEndDestination)
        {
            //Reached last waypoint
            pPhysicsVelocity.Linear = float3.zero;
        }

        float3 forwardRot;
        if (math.all(pPhysicsVelocity.Linear == float3.zero))
        {
            //Final rotation = group rotation
            forwardRot = pathFollow.groupMovement;
        }
        else
        {
            //Path rotation = towards next node
            forwardRot = nextPos - currentPos;
        }

        quaternion targetRot = quaternion.LookRotationSafe(forwardRot, new float3(0, 1, 0));
        pLocalTransform.Rotation = math.slerp(pLocalTransform.Rotation, targetRot, pathFollow.rotationSpeed * deltaTime);

        pLocalTransform.Position.y = pathFollow.yValue;
    }

    [BurstCompile]
    private bool PassedNode(float2 pFromNode, float2 pToNode, float2 pPosition)
    {
        float2 n = pFromNode - pToNode;
        float dotNV1 = math.dot(n, pPosition - pToNode);
        float dotNV2 = math.dot(n, pFromNode - pToNode);
        return (dotNV1 < 0) != (dotNV2 < 0);
    }
}