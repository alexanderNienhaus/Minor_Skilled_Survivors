using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitSelectionSystem))]
public partial class SelectedTankPathFindingSystem : SystemBase
{
    private bool nextMoveAttack;
    private string movementOrder;
    private string attackOrder;
    private string untis;

    protected override void OnCreate()
    {
        RequireForUpdate<SelectedUnitTag>();
        RequireForUpdate<FriendlyUnitCount>();
        nextMoveAttack = false;
        movementOrder = "Movement order issued for ";
        attackOrder = "Attack order issued for ";
        untis = " units!";
    }

    protected override void OnUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            nextMoveAttack = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            nextMoveAttack = false;
        }

        if (Input.GetMouseButtonDown(1) && PathfindingGridSetup.Instance != null)
        {
            GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
            int2 gridSize = new (grid.GetWidth(), grid.GetLength());
            float3 gridOriginPos = grid.GetOriginPos();
            float gridCellSize = grid.GetCellSize();
            float3 mouseEndPos = GetMouseWorldPos();
            mouseEndPos.y = 0;
            if (math.all(mouseEndPos == float3.zero))
                return;

            float3 groupStartPos = SystemAPI.GetSingleton<GroupPosition>().pos;
            float3 currentGroupMovement = mouseEndPos - groupStartPos;
            int selectedUnitCount = SystemAPI.GetSingleton<FriendlyUnitCount>().count;
            if (selectedUnitCount == 0)
                return;

            NativeArray<float3> endPositions = CalculateEndPositionOffsetsPointRotation(selectedUnitCount, gridCellSize * 4, currentGroupMovement, mouseEndPos);
            SetEndPositions(endPositions);

            FindTankPathJob findGroupPathJob = new ()
            {
                pathPositions = GetBufferLookup<PathPositions>(),
                pathNodeArray = GetPathNodeArray(grid, gridSize),
                gridSize = gridSize,
                gridOriginPos = gridOriginPos,
                gridCellSize = gridCellSize,
                groupMovement = currentGroupMovement,
                thetaStar = new ThetaStar(),
                isInAttackMode = nextMoveAttack
            };
            Dependency = findGroupPathJob.ScheduleParallel(Dependency);

            EventBus<OnInfoMenuTextChangeEvent>.Publish(new OnInfoMenuTextChangeEvent((nextMoveAttack ? attackOrder : movementOrder) + selectedUnitCount + untis));
            nextMoveAttack = false;
        }
    }

    private void SetEndPositions(NativeArray<float3> pEndPositions)
    {
        for (int i = 0; i < pEndPositions.Length; i++)
        {
            RefRW<FormationPosition> setFormation = default;
            float3 setPosition = float3.zero;
            float shortestDistance = float.MaxValue;
            foreach ((RefRW<FormationPosition> formationPosition, RefRO<LocalTransform> localTransform)
                in SystemAPI.Query<RefRW<FormationPosition>, RefRO<LocalTransform>>().WithAll<SelectedUnitTag>())
            {
                if (formationPosition.ValueRO.isSet)
                    continue;

                float currentDistance = math.lengthsq(pEndPositions[i] - localTransform.ValueRO.Position);
                if (currentDistance < shortestDistance)
                {
                    shortestDistance = currentDistance;
                    setPosition = pEndPositions[i];
                    setFormation = formationPosition;
                }
            }
            if (shortestDistance < float.MaxValue)
            {
                setFormation.ValueRW.position = setPosition;
                setFormation.ValueRW.isSet = true;
            }
        }

        foreach (RefRW<FormationPosition> formationPosition in SystemAPI.Query<RefRW<FormationPosition>>())
        {
            formationPosition.ValueRW.isSet = false;
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
        {
            return raycastHit.point;
        }
        else
        {
            return Vector3.zero;
        }
    }

    private NativeArray<PathNode> GetPathNodeArray(GridXZ<GridNode> pGrid, int2 pGridSize)
    {
        NativeArray<PathNode> pathNodeArray = new(pGridSize.x * pGridSize.y, Allocator.TempJob);

        for (int x = 0; x < pGridSize.x; x++)
        {
            for (int z = 0; z < pGridSize.y; z++)
            {
                PathNode pathNode = new ()
                {
                    x = x,
                    z = z,
                    index = x + z * pGridSize.x,

                    gCost = int.MaxValue,

                    isWalkable = pGrid.GetGridObject(x, z).GetIsWalkable(),
                    cameFromNodeIndex = -1
                };

                pathNodeArray[pathNode.index] = pathNode;
            }
        }

        return pathNodeArray;
    }

    [BurstCompile]
    private NativeArray<float3> CalculateEndPositionOffsetsPointRotation(int pSelectedUnitCount, float pSpread, float3 pCurrentGroupMovement, float3 pMousePos)
    {
        int boxSize = (int)math.ceil(math.sqrt(pSelectedUnitCount));
        int excessPositions = boxSize * boxSize - pSelectedUnitCount;

        NativeArray<float3> endPositionOffsets = new (boxSize * boxSize, Allocator.Temp);
        if (boxSize == 1)
        {
            endPositionOffsets[0] = pMousePos;
            return endPositionOffsets;
        }
        
        float groupRotation = math.atan2(pCurrentGroupMovement.z, pCurrentGroupMovement.x);

        int i = 0;
        int zMax = boxSize;
        int xMax = boxSize;
        float2 offset = new (xMax * 0.5f + 0.5f, zMax * 0.5f + 0.5f);

        for (int x = xMax; x > 0; x--)
        {
            int positionsToFill = xMax * x - xMax;
            if (excessPositions > positionsToFill)
            {
                offset.y += (excessPositions - positionsToFill) * 0.5f;
            }

            for (int z = zMax; z > 0; z--)
            {
                float2 pos = new (x, z);
                pos -= offset;
                pos *= pSpread;
                float2 rotatedPos = RotatePoint(pos, groupRotation);
                endPositionOffsets[i] = pMousePos + new float3(rotatedPos.x, 0, rotatedPos.y);
                i++;
            }
        }
        
        return endPositionOffsets;
    }

    [BurstCompile]
    private float2 RotatePoint(float2 pPointToRotate, float pAngle)
    {
        float cosTheta = math.cos(pAngle);
        float sinTheta = math.sin(pAngle);
        return new float2(cosTheta * pPointToRotate.x - sinTheta * pPointToRotate.y,
                          sinTheta * pPointToRotate.x + cosTheta * pPointToRotate.y);
    }
}
