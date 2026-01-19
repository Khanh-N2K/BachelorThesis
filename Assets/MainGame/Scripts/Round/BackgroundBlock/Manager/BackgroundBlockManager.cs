using System.Collections.Generic;
using UnityEngine;

public class BackgroundBlockManager : MonoBehaviour
{
    #region ___ REFERENCES ___
    private RoundManager _roundManager;

    public RoundManager RoundManager => _roundManager;

    private MapData _mapData => _roundManager.MapData;

    [Header("References")]

    [SerializeField]
    private BackgroundBlockSpawner _spawner;

    public BackgroundBlockSpawner Spawner => _spawner;
    #endregion ___


    #region ___ SETTINGS ___
    [Header("Settings")]

    [SerializeField]
    private BackgroundBlockConfigSO _configSO;

    public BackgroundBlockConfigSO ConfigSO => _configSO;
    #endregion ___


    #region ___ DATA ___
    private HashSet<Obstacle> _obstacleSet = new();

    private Dictionary<Vector2Int, Obstacle> _obstacleDict = new();
    #endregion ___


    public void Initialize(RoundManager roundManager)
    {
        _roundManager = roundManager;
        _spawner.Initialize(this);
    }

    public void RegisterObstacle(Vector2Int pos, Obstacle obstacle, MapBlockType blockType)
    {
        if (_obstacleSet.Contains(obstacle))
        {
            Debug.LogError($"There's a Obstacle at {pos} already.");
            return;
        }
        _mapData.UpdateBlockType(blockType, pos);
        _obstacleSet.Add(obstacle);
        _obstacleDict[pos] = obstacle;
    }

    public void UnregisterObstacle(Obstacle obstacle)
    {
        if (!_obstacleSet.Contains(obstacle))
        {
            Debug.LogError("Obstacle is not in the set already");
            return;
        }
        _obstacleSet.Remove(obstacle);
        _obstacleDict.Remove(obstacle.Coord);
    }

    public Obstacle GetObstacleAt(Vector2Int coord)
    {
        if (!_obstacleDict.ContainsKey(coord))
        {
            Debug.LogError("There's no obstacle at " + coord.ToString());
            return null;
        }
        return _obstacleDict[coord];
    }
}
