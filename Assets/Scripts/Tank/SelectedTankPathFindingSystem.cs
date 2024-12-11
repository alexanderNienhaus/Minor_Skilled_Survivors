using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile][UpdateAfter(typeof(UnitSelectionSystem))]
public partial class SelectedTankPathFindingSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SelectedUnitTag>();
    }

    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(1) && PathfindingGridSetup.Instance != null)
        {
            GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
            int2 gridSize = new int2(grid.GetWidth(), grid.GetLength());
            float3 gridOriginPos = grid.GetOriginPos();
            float gridCellSize = grid.GetCellSize();
            float3 mouseEndPos = GetMouseWorldPos();
            mouseEndPos.y = 0;
            if (math.all(mouseEndPos == float3.zero))
                return;

            SelectedUnitInformationSystem selectedUnitCountSystem = World.GetOrCreateSystemManaged<SelectedUnitInformationSystem>();
            selectedUnitCountSystem.UpdateSelectedUnitInfo();
            float3 groupStartPos = selectedUnitCountSystem.GetGroupStartPos();
            float3 currentGroupMovement = mouseEndPos - groupStartPos;
            int selectedUnitCount = selectedUnitCountSystem.GetSelectedUnitCount();

            NativeArray<float3> endPositions = CalculateEndPositionOffsetsPointRotation(selectedUnitCount, gridCellSize * 2, currentGroupMovement, mouseEndPos);
            SetEndPositions(endPositions);

            FindTankPathJob findGroupPathJob = new FindTankPathJob
            {
                pathPositions = GetBufferLookup<PathPositions>(),
                pathNodeArray = GetPathNodeArray(grid, gridSize),
                gridSize = gridSize,
                gridOriginPos = gridOriginPos,
                gridCellSize = gridCellSize,
                groupMovement = currentGroupMovement,
                thetaStar = new ThetaStar(),
                isInAttackMode = Input.GetKey(KeyCode.Space)
            };

            findGroupPathJob.ScheduleParallel();
        }
    }

    private void SetEndPositions(NativeArray<float3> endPositions)
    {
        for (int i = 0; i < endPositions.Length; i++)
        {
            RefRW<FormationPosition> setFormation = default;
            float3 setPosition = float3.zero;
            float shortestDistance = float.MaxValue;
            foreach ((RefRW<FormationPosition> formationPosition, RefRO<LocalTransform> localTransform)
                in SystemAPI.Query<RefRW<FormationPosition>, RefRO<LocalTransform>>().WithAll<SelectedUnitTag>())
            {
                if (formationPosition.ValueRO.isSet)
                {
                    continue;
                }

                float currentDistance = math.lengthsq(endPositions[i] - localTransform.ValueRO.Position);
                if (currentDistance < shortestDistance)
                {
                    shortestDistance = currentDistance;
                    setPosition = endPositions[i];
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
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(pGridSize.x * pGridSize.y, Allocator.Persistent);

        for (int x = 0; x < pGridSize.x; x++)
        {
            for (int z = 0; z < pGridSize.y; z++)
            {
                PathNode pathNode = new PathNode
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
    private NativeArray<float3> CalculateEndPositionOffsetsPointRotation(int selectedUnitCount, float pSpread, float3 currentGroupMovement, float3 mousePos)
    {
        int boxSize = (int)math.ceil(math.sqrt(selectedUnitCount));
        int excessPositions = boxSize * boxSize - selectedUnitCount;

        NativeArray<float3> endPositionOffsets = new NativeArray<float3>(boxSize * boxSize, Allocator.Persistent);
        if (boxSize == 1)
        {
            endPositionOffsets[0] = mousePos;
            return endPositionOffsets;
        }
        
        float groupRotation = math.atan2(currentGroupMovement.z, currentGroupMovement.x);

        int i = 0;
        int zMax = boxSize;
        int xMax = boxSize;
        float2 offset = new float2(xMax * 0.5f + 0.5f, zMax * 0.5f + 0.5f);

        for (int x = xMax; x > 0; x--)
        {
            int positionsToFill = xMax * x - xMax;
            if (excessPositions > positionsToFill)
            {
                offset.y += (excessPositions - positionsToFill) * 0.5f;
            }

            for (int z = zMax; z > 0; z--)
            {
                float2 pos = new float2(x, z);
                pos -= offset;
                pos *= pSpread;
                float2 rotatedPos = RotatePoint(pos, groupRotation);
                endPositionOffsets[i] = mousePos + new float3(rotatedPos.x, 0, rotatedPos.y);
                i++;
            }
        }
        
        return endPositionOffsets;
    }

    [BurstCompile]
    private float2 RotatePoint(float2 pointToRotate, float angle)
    {
        float cosTheta = math.cos(angle);
        float sinTheta = math.sin(angle);
        return new float2(cosTheta * pointToRotate.x - sinTheta * pointToRotate.y,
                          sinTheta * pointToRotate.x + cosTheta * pointToRotate.y);
    }
}
