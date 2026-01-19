using Cysharp.Threading.Tasks;
using DG.Tweening;
using N2K;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BackgroundBlockSpawner : MonoBehaviour
{
    #region ___ REFERENCES ___
    private BackgroundBlockManager _backgroundBlockManager;

    private MapData _mapData => _backgroundBlockManager.RoundManager.MapData;

    [Header("References")]

    [SerializeField]
    private Transform _objHolder;
    #endregion ___


    #region ___ SETTINGS ___
    [Header("Settings")]

    [SerializeField]
    private float _obstacleChance = 0.1f;
    #endregion ___


    #region ___ DATA ___

    private Vector2Int _mapSize => _mapData.MapSize;

    private MapBlockType[,] _mapMatrix => _mapData.MapMatrix;

    #endregion ___


    public void Initialize(BackgroundBlockManager backgroundBlockManager)
    {
        _backgroundBlockManager = backgroundBlockManager;
    }

    public async UniTask SpawnBackgroundObjs()
    {
        // Generate blocks data
        MapBlockType[,] blockMatrix = new MapBlockType[_mapSize.x, _mapSize.y];
        for (int y = 0; y < _mapSize.y; y++)
        {
            for (int x = 0; x < _mapSize.x; x++)
            {
                if (Random.value < _obstacleChance)
                {
                    blockMatrix[x, y] = MapBlockType.Obstacle;
                }
                else
                {
                    blockMatrix[x, y] = MapBlockType.Empty;
                }
            }
        }

        MapConnectivityUtility.FixClosedAreas(blockMatrix, _mapSize);

        await SpawnInsideBlocks(blockMatrix);
        //await SpawnOutsideBlocks();
    }

    async UniTask SpawnInsideBlocks(MapBlockType[,] blockMatrix)
    {
        // Divivde into groups to fast spawn, each group spawn after 0.1s seconds
        float blockAnimateTime = 0.3f;
        float groupSpawnInterval = 0.1f;
        float groupTotalSpawnTime = 1f;
        int groupCount = Mathf.FloorToInt(groupTotalSpawnTime / groupSpawnInterval);
        List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
        for (int i = 0; i < groupCount; i++)
        {
            groups.Add(new List<Vector2Int>());
        }
        // Random assign cells to groups
        for (int y = 0; y < _mapSize.y; y++)
        {
            for (int x = 0; x < _mapSize.x; x++)
            {
                int groupIndex = Random.Range(0, groupCount);
                groups[groupIndex].Add(new Vector2Int(x, y));
            }
        }
        foreach (var group in groups)
        {
            SpawnGroup(group, blockAnimateTime, blockMatrix);
            await UniTask.Delay(TimeSpan.FromSeconds(groupSpawnInterval));
        }
        await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Abs(blockAnimateTime - groupSpawnInterval)));

    }

    private void SpawnGroup(List<Vector2Int> cellList, float blockAnimateTime, MapBlockType[,] blockMatrix)
    {
        foreach (var cell in cellList)
        {
            // Spawn floor as default
            Vector3 worldPos = MapData.GetWorldPosOfCoord(cell);
            GameObject floor = Instantiate(_backgroundBlockManager.ConfigSO.RandomFloorPrefab, worldPos + Vector3.up * 15, Quaternion.identity, _objHolder);
            floor.transform.DOMove(worldPos, blockAnimateTime);
            _mapData.UpdateBlockType(MapBlockType.Empty, cell);
            // Check spawn obstacle
            if (blockMatrix[cell.y, cell.x] == MapBlockType.Obstacle)
            {
                Obstacle obstacle = ObjectPoolAtlas.Instance.Get<Obstacle>(_backgroundBlockManager.ConfigSO.RandomObstacle, _objHolder);
                obstacle.transform.position = worldPos + Vector3.up * 15;
                obstacle.transform.rotation = Quaternion.identity;
                obstacle.Initialize(_backgroundBlockManager, cell);
                obstacle.transform.DOMove(worldPos, blockAnimateTime);
                _backgroundBlockManager.RegisterObstacle(cell, obstacle, MapBlockType.Obstacle);
            }
        }
    }

    private async UniTask SpawnOutsideBlocks()
    {
        float waitTime = 0;
        // Top
        float moveTime = Random.Range(0.3f, 0.5f);
        waitTime = Mathf.Max(waitTime, moveTime);
        for (int x = -1; x <= _mapSize.x; x++)
        {
            Vector2Int pos = new Vector2Int(x, -1);
            Vector3 spawnPos = MapData.GetWorldPosOfCoord(pos);
            GameObject block = Instantiate(_backgroundBlockManager.ConfigSO.RandomBorderPrefab, spawnPos - Vector3.up * 2, Quaternion.identity, _objHolder);
            block.transform.DOMove(spawnPos, moveTime);
        }
        // Right
        moveTime = Random.Range(0.3f, 0.5f);
        waitTime = Mathf.Max(waitTime, moveTime);
        for (int y = 0; y < _mapSize.y; y++)
        {
            Vector2Int pos = new Vector2Int(_mapSize.x, y);
            Vector3 spawnPos = MapData.GetWorldPosOfCoord(new Vector2Int(_mapSize.x, y));
            GameObject block = Instantiate(_backgroundBlockManager.ConfigSO.RandomBorderPrefab, spawnPos - Vector3.up * 2, Quaternion.identity, _objHolder);
            block.transform.DOMove(spawnPos, moveTime);
        }
        // Bottom
        moveTime = Random.Range(0.3f, 0.5f);
        waitTime = Mathf.Max(waitTime, moveTime);
        for (int x = _mapSize.x; x >= -1; x--)
        {
            Vector2Int pos = new Vector2Int(x, _mapSize.y);
            Vector3 spawnPos = MapData.GetWorldPosOfCoord(new Vector2Int(x, _mapSize.y));
            GameObject block = Instantiate(_backgroundBlockManager.ConfigSO.RandomBorderPrefab, spawnPos - Vector3.up * 2, Quaternion.identity, _objHolder);
            block.transform.DOMove(spawnPos, moveTime);
        }
        // Left
        moveTime = Random.Range(0.3f, 0.5f);
        waitTime = Mathf.Max(waitTime, moveTime);
        for (int y = _mapSize.y - 1; y >= 0; y--)
        {
            Vector2Int pos = new Vector2Int(-1, y);
            Vector3 spawnPos = MapData.GetWorldPosOfCoord(new Vector2Int(-1, y));
            GameObject block = Instantiate(_backgroundBlockManager.ConfigSO.RandomBorderPrefab, spawnPos - Vector3.up * 2, Quaternion.identity, _objHolder);
            block.transform.DOMove(spawnPos, moveTime);
        }
        await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
    }
}
