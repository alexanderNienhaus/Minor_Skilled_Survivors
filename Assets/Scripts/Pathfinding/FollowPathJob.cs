using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics;

[BurstCompile]
public partial struct FollowpathJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public BufferLookup<PathPositions> allPathPositions;
    public float3 gridOriginPos;
    public float gridCellSize;
    public float deltaTime;

    [BurstCompile]
    public void Execute(Entity pEntity, ref LocalTransform pLocalTransform, ref PhysicsVelocity pPhysicsVelocity, ref PathFollow pathFollow)
    {
        if (!allPathPositions.HasBuffer(pEntity))
        {
            return;
        }

        DynamicBuffer<PathPositions> pathPositions = allPathPositions[pEntity];
        int length = pathPositions.Length;

        if (length >= 2)
        {
            float3 lastPos = pathPositions[length - 1].pos;
            float3 currentPos = pLocalTransform.Position;
            float3 nextPos = pathPositions[length - 2].pos;
            float3 moveVec = nextPos - currentPos;
            moveVec.y = 0;

            float3 moveDir = math.normalizesafe(moveVec, float3.zero);
            pPhysicsVelocity.Linear = moveDir * pathFollow.movementSpeed * deltaTime;


            if (length > 2)
            {
                if (PassedNode(new float2(lastPos.x, lastPos.z), new float2(nextPos.x, nextPos.z), new float2(currentPos.x, currentPos.z)))
                {
                    //Reached next waypoint
                    pathPositions.RemoveAt(length - 1);
                }
            }
            else if (length == 2)
            {
                if (math.distance(currentPos, nextPos) < pathFollow.checkDistanceFinal)
                {
                    //Reached last waypoint
                    pPhysicsVelocity.Linear = float3.zero;
                }
            }

            if (math.all(pPhysicsVelocity.Linear == float3.zero))
            {
                pLocalTransform.Rotation = math.slerp(pLocalTransform.Rotation, pathFollow.targetRotation, pathFollow.rotationSpeed * deltaTime);
            }
            else
            {
                quaternion targetRot = quaternion.LookRotationSafe(nextPos - currentPos, pLocalTransform.Up());
                pLocalTransform.Rotation = math.slerp(pLocalTransform.Rotation, targetRot, pathFollow.rotationSpeed * deltaTime);
            }
        }
    }

    [BurstCompile]
    private bool PassedNode(float2 fromNode, float2 toNode, float2 position)
    {
        float2 n = fromNode - toNode;
        float dotNV1 = math.dot(n, position - toNode);
        float dotNV2 = math.dot(n, fromNode - toNode);
        return (dotNV1 < 0) != (dotNV2 < 0);
    }
}