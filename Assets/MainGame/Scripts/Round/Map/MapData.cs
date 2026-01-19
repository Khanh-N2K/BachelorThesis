using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapData : MonoBehaviour
{
    #region ___ SETTINGS ___
    [Header("Settings")]

    [SerializeField]
    private Vector2Int _mapSize = new Vector2Int(20, 15);

    public Vector2Int MapSize => _mapSize;
    #endregion ___


    #region ___ DATA ___
    private MapBlockType[,] _mapMatrix;     // <y, x> <row, column>

    public MapBlockType[,] MapMatrix => _mapMatrix;
    #endregion ___

    public static Vector3 GetWorldPosOfCoord(Vector2Int coord)
    {
        return new Vector3(coord.x, 0, -coord.y);
    }

    public static Vector3 GetWorldPosOfCoord(int x, int y)
    {
        return new Vector3(x, 0, -y);
    }

    public static Vector2Int GetCoordOfWorldPos(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(-worldPos.z);
        return new Vector2Int(x, y);
    }

    public void GenerateMapData()
    {
        _mapMatrix = new MapBlockType[MapSize.y, MapSize.x];
    }

    public Vector3 GetCenterMapPos()
    {
        int x = Mathf.FloorToInt(_mapSize.x / 2);
        int y = Mathf.FloorToInt(_mapSize.y / 2);
        return GetWorldPosOfCoord(new Vector2Int(x, y));
    }

    public void UpdatePathFinderMatrix()
    {
        // Get specific matrix for pathfinder
        char[,] matrix = new char[_mapSize.y, _mapSize.x];
        for (int y = 0; y < _mapSize.y; y++)
        {
            for (int x = 0; x < _mapSize.x; x++)
            {
                if (_mapMatrix[y, x] == MapBlockType.Obstacle)
                {
                    matrix[y, x] = '@';
                }
                else
                {
                    matrix[y, x] = '.';
                }
            }
        }
        // Set map matrix
        PathFinder.SetMapMatrix(matrix);
    }

    public void UpdateBlockType(MapBlockType blockType, Vector2Int coord)
    {
        _mapMatrix[coord.y, coord.x] = blockType;
    }

    public void ReplaceAllBlockType(MapBlockType oldType, MapBlockType newType)
    {
        for(int y = 0; y < _mapSize.y; y++)
        {
            for(int x = 0; x < _mapSize.x; x++)
            {
                if (_mapMatrix[y, x] == oldType)
                {
                    _mapMatrix[y, x] = newType;
                }
            }
        }
    }

    public MapBlockType GetBlockTypeAt(Vector2Int pos)
    {
        return _mapMatrix[pos.y, pos.x];
    }

    public MapBlockType GetBlockTypeAt(int x, int y)
    {
        return _mapMatrix[y, x]; 
    }
}
