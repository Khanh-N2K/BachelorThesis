using Cysharp.Threading.Tasks;
using N2K;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class DefenderSpawner : MonoBehaviour
{
    #region ___ REFERENCES ___
    // External References

    private DefenderManager _defenderManager;

    private MapData _mapData => _defenderManager.RoundManager.MapData;

    [Header("References")]

    [SerializeField]
    private Transform _defenderHolder;
    #endregion ___


    #region ___ SETTINGS ___
    [Header("Settings")]

    [SerializeField]
    private Vector2 _spawnRange_HorizontalProportion = new Vector2(0.4f, 0.6f);

    [SerializeField]
    private Vector2 _spawnRange_VerticalProportion = new Vector2(0.4f, 0.6f);

    [SerializeField]
    private float _defenderChance = 0.3f;

    [SerializeField]
    private float _spawnInterval = 0.1f;
    #endregion ___


    #region ___ DATA ___
    // External Data

    private Vector2Int _mapSize => _mapData.MapSize;

    private MapBlockType[,] _mapMatrix => _mapData.MapMatrix;

    // Defender

    public Action onDefenderSpawned;
    #endregion ___


    public void Initialize(DefenderManager defenderManager)
    {
        _defenderManager = defenderManager;
    }

    public async UniTask SpawnRandomDefenders()
    {
        int xStart = Mathf.FloorToInt(_mapSize.x * _spawnRange_HorizontalProportion.x);
        int xEnd = Mathf.FloorToInt(_mapSize.x * _spawnRange_HorizontalProportion.y);
        xStart = Mathf.Clamp(xStart, 0, _mapSize.x - 1);
        xEnd = Mathf.Clamp(xEnd, 0, _mapSize.x - 1);
        int yStart = Mathf.FloorToInt(_mapSize.y * _spawnRange_VerticalProportion.x);
        int yEnd = Mathf.FloorToInt(_mapSize.y * _spawnRange_VerticalProportion.y);
        yStart = Mathf.Clamp(yStart, 0, _mapSize.y - 1);
        yEnd = Mathf.Clamp(yEnd, 0, _mapSize.y - 1);
        Vector2Int coord;
        Defender prefab;
        for (int y = yStart; y <= yEnd; y++)
        {
            for (int x = xStart; x <= xEnd; x++)
            {
                if (Random.value < _defenderChance && _mapMatrix[y, x] == MapBlockType.Empty)
                {
                    prefab = _defenderManager.ConfigSO.GetRandomPrefab();
                    Defender defender = ObjectPoolAtlas.Instance.Get(prefab, _defenderHolder);
                    coord = new Vector2Int(x, y);
                    defender.Initialize(_defenderManager, coord);
                    defender.transform.position = MapData.GetWorldPosOfCoord(coord);
                    defender.transform.rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
                    _defenderManager.RegisterDefender(defender);
                    _mapData.UpdateBlockType(MapBlockType.Defender, coord);
                    await UniTask.Delay(TimeSpan.FromSeconds(_spawnInterval));
                }
            }
        }
        onDefenderSpawned?.Invoke();
    }
}
