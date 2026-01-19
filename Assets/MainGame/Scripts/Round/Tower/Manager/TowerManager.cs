using N2K;
using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    #region ___ REFERENCES ___

    // External references

    private RoundManager _roundManager;

    public RoundManager RoundManager => _roundManager;

    [Header("References")]

    [SerializeField]
    private TowerConfigSO _configSO;

    public TowerConfigSO ConfigSO => _configSO;

    [SerializeField]
    private Transform _holder;

    [Header("References - Vfx")]

    [SerializeField]
    private PooledVfx _damageBoostVfxPrefab;

    public PooledVfx DamageBoostVfxPrefab => _damageBoostVfxPrefab;

    [SerializeField]
    private PooledVfx _fireRateBoostVfxPrefab;

    public PooledVfx FireRateBoostedVfxPrefab => _fireRateBoostVfxPrefab;

    #endregion ___

    #region ___ DATA  ___

    private HashSet<Tower> _towerSet = new();

    #endregion ___

    public void Initialize(RoundManager roundManager)
    {
        _roundManager = roundManager;
    }

    #region ___ TOWER SET ___

    public void RegisterTower(Tower tower)
    {
        if (_towerSet.Contains(tower))
        {
            Debug.LogError($"Tower {tower.name} is in the set already");
            return;
        }
        _towerSet.Add(tower);
    }

    public void UnregisterTower(Tower tower)
    {
        if (!_towerSet.Contains(tower))
        {
            Debug.LogError($"Tower {tower.name} is not in the set");
            return;
        }
        _towerSet.Remove(tower);
    }

    #endregion ___

    public Tower PlaceNewTowerOnObstacle(TowerType towerType, int level, Obstacle obstacle)
    {
        // Prepare the placement
        if (obstacle.HasTower)
        {
            Debug.LogError("There's a tower on obstacle already");
            return null;
        }
        obstacle.SetShowDecorateObjs(show: false);
        // Spawn tower
        TowerLevelConfig? towerLevelConfig = _configSO.GetTowerLevelConfig(towerType, level);
        Tower tower = ObjectPoolAtlas.Instance.Get<Tower>(towerLevelConfig.Value.towerPrefab, _holder);
        tower.Initialize(towerType, towerLevelConfig.Value, this, obstacle);
        RegisterTower(tower);
        // Place tower
        obstacle.RegisterTower(tower);
        return tower;
    }

    #region ___ TOWER STATE ___

    public void ChangeTowersState(TowerState state)
    {
        foreach (var tower in _towerSet)
        {
            tower.ChangeState(state);
        }
    }

    #endregion ___
}
