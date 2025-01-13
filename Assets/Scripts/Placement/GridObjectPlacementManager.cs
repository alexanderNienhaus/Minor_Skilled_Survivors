using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class GridObjectPlacementManager : MonoBehaviour
{
    public static GridObjectPlacementManager Instance { get { return _instance; } }
    private static GridObjectPlacementManager _instance;

    [SerializeField] private List<PlacableObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private LayerMask mouseColliderMask;

    [Header("GUI Info Messages")]
    [SerializeField] private string objectPlacedSuccessfully;
    [SerializeField] private string notEnoughResources;
    [SerializeField] private string cantBuildHere;
    [SerializeField] private string notInBuildingPhase;

    [Header("Grid")]
    [SerializeField] private int gridWidth = 100;
    [SerializeField] private int gridLength = 100;
    [SerializeField] private float cellSize = 2;
    [SerializeField] private Vector3 originPos = new Vector3(-100, 0, -100);

    [Header("Debugging")]
    [SerializeField] private bool gridShowDebug = false;
    [SerializeField] private int gridDebugTextSize = 10;
    [SerializeField] private bool allowDebugBuildings = false;

    private GridXZ<GridObject> grid;
    private PlacableObjectTypeSO placedObjectTypeSO;
    private PlacableObjectTypeSO.Dir direction = PlacableObjectTypeSO.Dir.Right;
    private PlacedEntityManagementSystem placableObjectsSpawningSystem;
    private int placedObjectIndex;

    public bool GetCanBuild()
    {
        if (placedObjectTypeSO == null)
            return false;

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
        return Quaternion.Euler(0, PlacableObjectTypeSO.GetRotationAngle(direction), 0);
    }

    public PlacableObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }

    public void SetDirection(PlacableObjectTypeSO.Dir pDirection)
    {
        direction = pDirection;
    }

    public void SetPlacedObjectTypeSO(int pType)
    {
        placedObjectTypeSO = placedObjectTypeSOList[pType];
    }

    public List<Vector2Int> GetGridPosList(int pX, int pZ)
    {
        return placedObjectTypeSO.GetGridPosList(new Vector2Int(pX, pZ), direction);
    }

    public void GetGridPosFromWorldPos(Vector3 pWorldPos, out int pX, out int pZ)
    {
        grid.GetXZ(pWorldPos, out pX, out pZ);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
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
        direction = PlacableObjectTypeSO.Dir.Down;

        if (Input.GetMouseButtonDown(0) && placedObjectTypeSO != null)
        {
            GetGridPosFromWorldPos(GetMouseWorldPos(), out int x, out int z);
            List<Vector2Int> gridPosList = GetGridPosList(x, z);

            BuildIfPreconditionsMatch(x, z, gridPosList);
        }

        if (Input.GetMouseButtonDown(2))
        {
            DestroyBuilding();
        }

        if (Input.GetKeyDown(KeyCode.R) && allowDebugBuildings)
        {
            direction = PlacableObjectTypeSO.GetNextDir(direction);
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

        if (Input.GetKeyDown(KeyCode.Alpha3) && allowDebugBuildings)
        {
            placedObjectTypeSO = placedObjectTypeSOList[2];
            EventBus<OnSelectedPlacableObjectChanged>.Publish(new OnSelectedPlacableObjectChanged());
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) && allowDebugBuildings)
        {
            placedObjectTypeSO = placedObjectTypeSOList[3];
            EventBus<OnSelectedPlacableObjectChanged>.Publish(new OnSelectedPlacableObjectChanged());
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            placedObjectTypeSO = null;
        }
    }

    private void BuildIfPreconditionsMatch(int pX, int pZ, List<Vector2Int> pGridPosList)
    {
        if (!World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimerSystem>().IsBuildTime())
        {
            EventBus<OnInfoMenuTextChangeEvent>.Publish(new OnInfoMenuTextChangeEvent(notInBuildingPhase));
            return;
        }

        if (!CanBuild(pGridPosList))
        {
            EventBus<OnInfoMenuTextChangeEvent>.Publish(new OnInfoMenuTextChangeEvent(cantBuildHere));
            return;
        }

        if (World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RessourceSystem>().GetResourceCount() < placedObjectTypeSO.cost)
        {
            EventBus<OnInfoMenuTextChangeEvent>.Publish(new OnInfoMenuTextChangeEvent(notEnoughResources));
            return;
        }

        EventBus<OnResourceChangedEvent>.Publish(new OnResourceChangedEvent(-placedObjectTypeSO.cost));
        EventBus<OnInfoMenuTextChangeEvent>.Publish(new OnInfoMenuTextChangeEvent(objectPlacedSuccessfully));

        if (placedObjectTypeSO.type == AttackableUnitType.RadioStation)
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WaveSystem>().QueueRadioStationSpawn();

        PlaceBuilding(pX, pZ, pGridPosList);
    }

    private bool CanBuild(List<Vector2Int> pGridPosList)
    {
        foreach (Vector2Int gridPos in pGridPosList)
        {
            GridObject gridObject = grid.GetGridObject(gridPos.x, gridPos.y);
            if (gridObject != null && !gridObject.CanBuild())
            {
                return false;
            }
        }
        return true;
    }

    public void PlaceBuilding(int pX, int pZ, List<Vector2Int> pGridPosList, bool pCreateEntity = true)
    {
        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(direction);
        float cellSize = grid.GetCellSize();

        Vector3 placedObjectWorldPos = grid.GetWorldPositionXZ(pX, pZ)
            + new Vector3(rotationOffset.x, 0, rotationOffset.y) * cellSize - new Vector3(0.5f, 0, 0.5f) * cellSize;
        PlacedObject placedObject = new PlacedObject(placedObjectTypeSO, new Vector2Int(pX, pZ), direction);

        int id = placedObjectIndex++;

        if (pCreateEntity)
        {
            placableObjectsSpawningSystem.CreateEntity(placedObjectTypeSO.index, placedObjectWorldPos,
                Quaternion.Euler(0, PlacableObjectTypeSO.GetRotationAngle(direction), 0), placedObjectTypeSO.scale, id);
        }

        ProjectOntoPathfindingGrid(placedObject.GetPlacedObjectTypeSO(), pGridPosList, false);
        placedObject.SetId(id);

        foreach (Vector2Int gridPos in pGridPosList)
        {
            GridObject gridObject = grid.GetGridObject(gridPos.x, gridPos.y);
            if(gridObject == null)
                continue;

            gridObject.SetPlacedObject(placedObject);
        }

        placedObjectTypeSO = null;
        EventBus<OnSelectedPlacableObjectChanged>.Publish(new OnSelectedPlacableObjectChanged());
    }

    private void DestroyBuilding()
    {
        GridObject gridObject = grid.GetGridObject(GetMouseWorldPos());
        if (gridObject == null)
            return;

        PlacedObject placedObject = gridObject.GetPlacedObject();
        if (placedObject == null)
            return;

        List<Vector2Int> gridPosList = placedObject.GetGridPosList();
        foreach (Vector2Int gridPos in gridPosList)
        {
            grid.GetGridObject(gridPos.x, gridPos.y).ClearPlacedObject();
        }
        ProjectOntoPathfindingGrid(placedObject.GetPlacedObjectTypeSO(), gridPosList, true);

        placableObjectsSpawningSystem.DestroyEntity(placedObject.GetId());
    }
    
    private void ProjectOntoPathfindingGrid(PlacableObjectTypeSO pPlacedObjectTypeSO, List<Vector2Int> pGridPositions, bool pSetIsWalkable)
    {
        GridXZ<GridNode> pathfindingGrid = PathfindingGridSetup.Instance.pathfindingGrid;

        Vector3 bottomLeftCornerWorldPos = grid.GetWorldPositionXZ(pGridPositions[0].x, pGridPositions[0].y)
            - new Vector3(1, 0, 1) * cellSize * 0.5f
            + new Vector3(1, 0, 1) * pathfindingGrid.GetCellSize() * 0.5f;
        pathfindingGrid.GetXZ(bottomLeftCornerWorldPos, out int pathfindingfGridBottomLeftCellX, out int pathfindingfGridBottomLeftCellZ);

        int width = direction == PlacableObjectTypeSO.Dir.Down || direction == PlacableObjectTypeSO.Dir.Up ? pPlacedObjectTypeSO.width : pPlacedObjectTypeSO.length;
        int length = direction == PlacableObjectTypeSO.Dir.Down || direction == PlacableObjectTypeSO.Dir.Up ? pPlacedObjectTypeSO.length : pPlacedObjectTypeSO.width;
        float cellSizeFactor = cellSize / pathfindingGrid.GetCellSize();
        float xMax = pathfindingfGridBottomLeftCellX + width * cellSizeFactor;
        float zMax = pathfindingfGridBottomLeftCellZ + length * cellSizeFactor;

        for (int x = pathfindingfGridBottomLeftCellX - 1; x <= xMax - 1; x++)
        {
            for (int z = pathfindingfGridBottomLeftCellZ - 1; z <= zMax - 1; z++)
            {
                GridNode gridNode = pathfindingGrid.GetGridObject(x, z);
                if (gridNode == null)
                    continue;

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
