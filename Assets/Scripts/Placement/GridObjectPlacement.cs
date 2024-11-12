using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using Unity.Entities;

public class GridObjectPlacement : MonoBehaviour
{
    public static GridObjectPlacement Instance { get { return _instance; } }
    private static GridObjectPlacement _instance;

    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private LayerMask mouseColliderMask;

    [Header("Grid")]
    [SerializeField] private int gridWidth = 100;
    [SerializeField] private int gridLength = 100;
    [SerializeField] private float cellSize = 2;
    [SerializeField] private Vector3 originPos = new Vector3(-100, 0, -100);

    [Header("Debugging")]
    [SerializeField] private bool gridShowDebug = false;
    [SerializeField] private int gridDebugTextSize = 10;

    private GridXZ<GridObject> grid;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir direction = PlacedObjectTypeSO.Dir.Down;
    private PlacedEntityManagementSystem placableObjectsSpawningSystem;
    private int placedObjectIndex;

    public bool GetCanBuild()
    {
        if (placedObjectTypeSO == null)
        {
            return false;
        }

        grid.GetXZ(GetMouseWorldPos(), out int x, out int z);
        List<Vector2Int> gridPosList = placedObjectTypeSO?.GetGridPosList(new Vector2Int(x, z), direction);
        return CanBuild(gridPosList);
    }

    public Vector3 GetMouseWorldSnappedPos()
    {
        grid.GetXZ(GetMouseWorldPos(), out int x, out int z);
        Vector2Int rotationOffset = Vector2Int.zero;
        if (placedObjectTypeSO != null)
        {
            rotationOffset = placedObjectTypeSO.GetRotationOffset(direction);
        }
        return grid.GetWorldPositionXZ(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
    }

    public float GetCellSize()
    {
        return grid.GetCellSize();
    }

    public Quaternion GetPlacedObjectRotation()
    {
        return Quaternion.Euler(0, PlacedObjectTypeSO.GetRotationAngle(direction), 0);
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        grid = new GridXZ<GridObject>(gridWidth, gridLength, cellSize, originPos, gridShowDebug, gridDebugTextSize,
            (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z));
        placedObjectTypeSO = null;
        placableObjectsSpawningSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlacedEntityManagementSystem>();
        placedObjectIndex = 0;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && placedObjectTypeSO != null)
        {
            grid.GetXZ(GetMouseWorldPos(), out int x, out int z);
            List<Vector2Int> gridPosList = placedObjectTypeSO.GetGridPosList(new Vector2Int(x, z), direction);

            if (CanBuild(gridPosList))
            {
                PlaceBuilding(x, z, gridPosList);
            }
            else
            {
                UtilsClass.CreateWorldTextPopup("Cannot build here!", GetMouseWorldPos());
            }
        }

        if (Input.GetMouseButtonDown(2))
        {
            DestroyBuilding();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            direction = PlacedObjectTypeSO.GetNextDir(direction);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            placedObjectTypeSO = placedObjectTypeSOList[0];
            EventBus<OnSelectedPlacableObjectChanged>.Publish(new OnSelectedPlacableObjectChanged());
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            placedObjectTypeSO = placedObjectTypeSOList[1];
            EventBus<OnSelectedPlacableObjectChanged>.Publish(new OnSelectedPlacableObjectChanged());
        }       
    }

    private bool CanBuild(List<Vector2Int> gridPosList)
    {
        foreach (Vector2Int gridPos in gridPosList)
        {
            GridObject gridObject = grid.GetGridObject(gridPos.x, gridPos.y);
            if (gridObject != null && !gridObject.CanBuild())
            {
                return false;
            }
        }

        return true;
    }

    private void PlaceBuilding(int x, int z, List<Vector2Int> gridPosList)
    {
        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(direction);
        Vector3 placedObjectWorldPos = grid.GetWorldPositionXZ(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize()- new Vector3(0.5f, 0, 0.5f) * grid.GetCellSize();
        PlacedObject placedObject = new PlacedObject(placedObjectTypeSO, new Vector2Int(x, z), direction);

        int id = placedObjectIndex++;
        placableObjectsSpawningSystem.CreateEntity(placedObjectTypeSO.index, placedObjectWorldPos,
            Quaternion.Euler(0, PlacedObjectTypeSO.GetRotationAngle(direction), 0), id);
        ProjectOntoPathfindingGrid(placedObject.GetPlacedObjectTypeSO(), gridPosList, false);
        placedObject.SetId(id);

        foreach (Vector2Int gridPos in gridPosList)
        {
            GridObject gridObject = grid.GetGridObject(gridPos.x, gridPos.y);
            if(gridObject == null)
            {
                //Debug.Log("SKIP");
                continue;
            }
            gridObject.SetPlacedObject(placedObject);
        }

        placedObjectTypeSO = null;
        EventBus<OnSelectedPlacableObjectChanged>.Publish(new OnSelectedPlacableObjectChanged());
    }

    private void DestroyBuilding()
    {
        GridObject gridObject = grid.GetGridObject(GetMouseWorldPos());
        if (gridObject == null)
        {
            return;
        }

        PlacedObject placedObject = gridObject.GetPlacedObject();
        if (placedObject == null)
        {
            return;
        }

        List<Vector2Int> gridPosList = placedObject.GetGridPosList();
        foreach (Vector2Int gridPos in gridPosList)
        {
            grid.GetGridObject(gridPos.x, gridPos.y).ClearPlacedObject();
        }
        ProjectOntoPathfindingGrid(placedObject.GetPlacedObjectTypeSO(), gridPosList, true);

        placableObjectsSpawningSystem.DestroyEntity(placedObject.GetId());
    }
    
    private void ProjectOntoPathfindingGrid(PlacedObjectTypeSO pPlacedObjectTypeSO, List<Vector2Int> pGridPositions, bool pSetIsWalkable)
    {
        GridXZ<GridNode> pathfindingGrid = PathfindingGridSetup.Instance.pathfindingGrid;

        Vector3 bottomLeftCornerWorldPos = grid.GetWorldPositionXZ(pGridPositions[0].x, pGridPositions[0].y)
            - new Vector3(1, 0, 1) * cellSize * 0.5f
            + new Vector3(1, 0, 1) * pathfindingGrid.GetCellSize() * 0.5f;
        pathfindingGrid.GetXZ(bottomLeftCornerWorldPos, out int pathfindingfGridBottomLeftCellX, out int pathfindingfGridBottomLeftCellZ);

        int width = direction == PlacedObjectTypeSO.Dir.Down || direction == PlacedObjectTypeSO.Dir.Up ? pPlacedObjectTypeSO.width : pPlacedObjectTypeSO.length;
        int length = direction == PlacedObjectTypeSO.Dir.Down || direction == PlacedObjectTypeSO.Dir.Up ? pPlacedObjectTypeSO.length : pPlacedObjectTypeSO.width;
        float cellSizeFactor = cellSize / pathfindingGrid.GetCellSize();
        float xMax = pathfindingfGridBottomLeftCellX + width * cellSizeFactor;
        float zMax = pathfindingfGridBottomLeftCellZ + length * cellSizeFactor;

        for (int x = pathfindingfGridBottomLeftCellX - 1; x <= xMax - 1; x++)
        {
            for (int z = pathfindingfGridBottomLeftCellZ - 1; z <= zMax - 1; z++)
            {
                GridNode gridNode = pathfindingGrid.GetGridObject(x, z);
                if (gridNode == null)
                {
                    //Debug.Log("SKIP");
                    continue;
                }
                gridNode.SetIsWalkable(pSetIsWalkable);
            }
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
}
