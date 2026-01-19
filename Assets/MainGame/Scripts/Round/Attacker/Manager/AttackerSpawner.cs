using Cysharp.Threading.Tasks;
using N2K;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskBand;
using static UnityEngine.Rendering.STP;
using Random = UnityEngine.Random;

public class AttackerSpawner : MonoBehaviour
{
    #region ___ REFERENCES ___

    // External references

    private AttackerManager _attackerManager;

    private MapData _mapData => _attackerManager.RoundManager.MapData;

    [Header("References")]

    [SerializeField]
    private Transform _holder;

    [SerializeField]
    private AttackerGroup _groupPrefab;

    public AttackerGroup GroupPrefab => _groupPrefab;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private float _spawnInterval = 0.2f;

    #endregion ___

    #region ___ DATA ___

    // External data

    private Vector2Int _mapSize => _mapData.MapSize;

    #endregion ___

    public void Initialize(AttackerManager attackerManager)
    {
        _attackerManager = attackerManager;
    }

    #region ___ SPAWN ENEMIES ___

    public async UniTask SpawnAttackers()
    {
        // Get spawn counts
        AttackerSpawnConfig spawnConfig = _attackerManager.ConfigSO.GetSpawnConfig(_attackerManager.RoundManager.CurrentWave).Value;
        int attackerCount = spawnConfig.attackerCount;
        int spawnPosCount = spawnConfig.spawnPosCount;

        // Randomly divide population into spawn pos
        int[] spawnPosPopulationArr = DivideRandomly(attackerCount, spawnPosCount);

        // Spawn in different positions
        foreach (int spawnPosPopulation in spawnPosPopulationArr)
        {
            await SpawnAttackersInRandomPos(spawnPosPopulation);
        }
    }

    private async UniTask SpawnAttackersInRandomPos(int count)
    {
        // Select spawn center and focus cam
        Vector2Int spawnCenter = GetRandomSpawnPos();
        bool isCamFocusFinished = false;
        GameManager.Instance.TopdownCam.StartFocusTo(MapData.GetWorldPosOfCoord(spawnCenter), 60, onClosedToTargetFirstTime:
            () => isCamFocusFinished = true);
        await UniTask.WaitUntil(() => isCamFocusFinished);

        // Loop in square rings from that center to spawn 'count' groups of 1 attacker each
        int maxRadius = Mathf.Max(spawnCenter.x, spawnCenter.y, _mapSize.x - 1 - spawnCenter.x, _mapSize.y - 1 - spawnCenter.y);
        for (int r = 0; r <= maxRadius; r++)
        {
            bool anyInside = false;

            for (int dx = -r; dx <= r; dx++)
            {
                int x1 = spawnCenter.x + dx;
                int y1 = spawnCenter.y - r;
                int y2 = spawnCenter.y + r;
                if (x1 >= 0 && x1 < _mapSize.x)
                {
                    if (y1 >= 0 && y1 < _mapSize.y)
                    {
                        anyInside = true;
                        if (TrySpawnAttackerGroup(x1, y1, 1))
                        {
                            count--;
                            if (count <= 0)
                            {
                                await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                                return;
                            }
                        }
                        await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                    }
                    if (r > 0 && y2 >= 0 && y2 < _mapSize.y)
                    {
                        anyInside = true;
                        if (TrySpawnAttackerGroup(x1, y2, 1))
                        {
                            count--;
                            if (count <= 0)
                            {
                                await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                                return;
                            }
                        }
                        await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                    }
                }
            }

            for (int dy = -r + 1; dy <= r - 1; dy++)
            {
                int y = spawnCenter.y + dy;
                int x1 = spawnCenter.x - r;
                int x2 = spawnCenter.x + r;
                if (y >= 0 && y < _mapSize.y)
                {
                    if (x1 >= 0 && x1 < _mapSize.x)
                    {
                        anyInside = true;
                        if (TrySpawnAttackerGroup(x1, y, 1))
                        {
                            count--;
                            if (count <= 0)
                            {
                                await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                                return;
                            }
                        }
                        await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                    }
                    if (r > 0 && x2 >= 0 && x2 < _mapSize.x)
                    {
                        anyInside = true;
                        if (TrySpawnAttackerGroup(x2, y, 1))
                        {
                            count--;
                            if (count <= 0)
                            {
                                await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                                return;
                            }
                        }
                        await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                    }
                }
            }

            if (!anyInside)
            {
                break;
            }
        }
    }

    private bool TrySpawnAttackerGroup(int x, int y, int attackerCount)
    {
        Vector2Int coord = new Vector2Int(x, y);

        // Check valid block type
        MapBlockType blockType = _mapData.GetBlockTypeAt(x, y);
        if (blockType == MapBlockType.Obstacle)
        {
            Obstacle obstacle = _attackerManager.RoundManager.BackgroundBlockManager.GetObstacleAt(coord);
            if (!obstacle.HasTower)
            {
                obstacle.DestroySelf();
            }
            else
            {
                return false;
            }
        }
        else if (blockType == MapBlockType.AttackerGroup || blockType == MapBlockType.Defender)
        {
            return false;
        }
        else if (blockType != MapBlockType.Empty)
        {
            Debug.LogError("Not implemented!");
            return false;
        }

        // Spawn attacker group
        AttackerGroup group = SpawnNewGroup(coord);
        group.SpawnAttackers(1);
        return true;
    }

    public AttackerGroup SpawnNewGroup(Vector2Int coord)
    {
        AttackerGroup group = ObjectPoolAtlas.Instance.Get(_groupPrefab, _holder);
        //AttackerGroup group = Instantiate(_groupPrefab, _holder);
        group.Initialize(_attackerManager, _attackerManager.GetNewAttackerGroupId(), coord);
        _attackerManager.RegisterAttackerGroup(group);
        _mapData.UpdateBlockType(MapBlockType.AttackerGroup, coord);
        return group;
    }

    #endregion ___

    #region ___ RANDOM POSITION ___

    private static int[] DivideRandomly(int total, int groupCount)
    {
        if (groupCount <= 0)
            return System.Array.Empty<int>();

        int baseSize = total / groupCount;
        int remainder = total % groupCount;

        int[] result = new int[groupCount];

        // Assign base size
        for (int i = 0; i < groupCount; i++)
        {
            result[i] = baseSize;
        }

        // Create index list
        List<int> indices = new List<int>(groupCount);
        for (int i = 0; i < groupCount; i++)
        {
            indices.Add(i);
        }

        // Shuffle indices (Fisher–Yates)
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Distribute remainder randomly
        for (int i = 0; i < remainder; i++)
        {
            result[indices[i]]++;
        }

        return result;
    }

    private Vector2Int GetRandomSpawnPos()
    {
        int spawnRangeX = 10;   // spawn ranges from border
        int spawnRangeY = 10;   // ...
        int randomArea = Random.Range(0, 4);
        int x, y;
        if (randomArea == 0)     // Top area
        {
            x = Random.Range(0, _mapSize.x);
            y = Random.Range(_mapSize.y - spawnRangeY, _mapSize.y);
        }
        else if (randomArea == 1)    // Right area
        {
            x = Random.Range(_mapSize.x - spawnRangeX, _mapSize.x);
            y = Random.Range(0, _mapSize.y);
        }
        else if (randomArea == 2)     // Bot area
        {
            x = Random.Range(0, _mapSize.x);
            y = Random.Range(0, spawnRangeY);
        }
        else    // Left area
        {
            x = Random.Range(0, spawnRangeX);
            y = Random.Range(0, _mapSize.y);
        }
        return new Vector2Int(x, y);
    }
    #endregion ___
}
