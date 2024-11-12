using UnityEngine;

//Setup for the pathfinding grid
public class PathfindingGridSetup : MonoBehaviour
{
    public static PathfindingGridSetup Instance { private set; get; }

    [HideInInspector] public GridXZ<GridNode> pathfindingGrid;

    [SerializeField] private Vector3 originPos = new Vector3(-100, 0, -100);
    [SerializeField] private bool gridShowDebug = false;
    [SerializeField] private int gridDebugTextSize = 30;
    [SerializeField] private int gridWidth = 100;
    [SerializeField] private int gridLength = 100;
    [SerializeField] private float cellSize = 2;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathfindingGrid = new GridXZ<GridNode>(gridWidth, gridLength, cellSize, originPos, gridShowDebug, gridDebugTextSize, (GridXZ<GridNode> grid, int x, int y) => new GridNode(grid, x, y));
    }
}
