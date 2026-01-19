using N2K;
using UnityEngine;

public class Obstacle : PoolMember
{
    #region ___ REFERENCES ___
    // EXTERNAL REFERENCES
    private BackgroundBlockManager _backgroundBlockManager;

    [Header("=== OBSTACLE ===")]

    [Header("References")]

    [SerializeField]
    private ObstacleSelectablePart _selectablePart;

    [SerializeField]
    private GameObject[] _decorateObjArr;
    #endregion ___

    #region ___ SETTINGS ___
    protected override int defaultCapacity => 30;

    protected override int maxSize => 40;
    #endregion ___

    #region ___ DATA ___
    private Vector2Int _coord;

    public Vector2Int Coord => _coord;

    private Tower _tower;

    public bool HasTower => _tower != null;
    #endregion ___

    public void Initialize(BackgroundBlockManager backgroundBlockManager, Vector2Int coord)
    {
        _backgroundBlockManager = backgroundBlockManager;
        _coord = coord;
        _tower = null;
        SetShowDecorateObjs();
        _selectablePart.Initialize();
        _selectablePart.Initialize(this);
    }

    public void DestroySelf()
    {
        if (_tower != null)
        {
            _tower.DestroySelf();
            _tower = null;
        }
        _backgroundBlockManager.UnregisterObstacle(this);
        _backgroundBlockManager.RoundManager.MapData.UpdateBlockType(MapBlockType.Empty, _coord);
        ReleaseToPool();
    }

    public void SetShowDecorateObjs(bool show = true)
    {
        for (int i = 0; i < _decorateObjArr.Length; i++)
        {
            _decorateObjArr[i].SetActive(show);
        }
    }

    public void RegisterTower(Tower tower)
    {
        if (_tower != null)
        {
            Debug.LogError($"There's a tower ({_tower.name}) here already");
            return;
        }
        _tower = tower;
        _selectablePart.SetSelectable(false);
        _tower.transform.position = transform.position + Vector3.up * 0.15f;
    }

    public void UnregisterTower()
    {
        if (_tower == null)
        {
            return;
        }
        _tower = null;
        _selectablePart.SetSelectable(true);
    }
}
