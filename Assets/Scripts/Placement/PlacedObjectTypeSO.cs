using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PlacedObjectTypeSO")]
public class PlacedObjectTypeSO : ScriptableObject
{
    public new string name;
    public int index;
    public Transform visual;
    public int width;
    public int length;

    public enum Dir
    {
        Down,
        Left,
        Up,
        Right
    }

    public static Dir GetNextDir(Dir pDir)
    {
        switch (pDir)
        {
            default:
            case Dir.Down:
                return Dir.Left;
            case Dir.Left:
                return Dir.Up;
            case Dir.Up:
                return Dir.Right;
            case Dir.Right:
                return Dir.Down;
        }
    }

    public static int GetRotationAngle(Dir pDir)
    {
        switch (pDir)
        {
            default:
            case Dir.Down:
                return 0;
            case Dir.Left:
                return 90;
            case Dir.Up:
                return 180;
            case Dir.Right:
                return 270;
        }
    }

    public Vector2Int GetRotationOffset(Dir pDir)
    {
        switch (pDir)
        {
            default:
            case Dir.Down:
                return new Vector2Int(0, 0);
            case Dir.Left:
                return new Vector2Int(0, width);
            case Dir.Up:
                return new Vector2Int(width, length);
            case Dir.Right:
                return new Vector2Int(length, 0);
        }
    }

    public List<Vector2Int> GetGridPosList(Vector2Int pOffset, Dir pDirection)
    {
        List<Vector2Int> gridPosList = new List<Vector2Int>();
        switch (pDirection)
        {
            default:
            case Dir.Down:
            case Dir.Up:
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        gridPosList.Add(pOffset + new Vector2Int(x, z));
                    }
                }
                break;
            case Dir.Left:
            case Dir.Right:
                for (int x = 0; x < length; x++)
                {
                    for (int z = 0; z < width; z++)
                    {
                        gridPosList.Add(pOffset + new Vector2Int(x, z));
                    }
                }
                break;
        }
        return gridPosList;
    }
}