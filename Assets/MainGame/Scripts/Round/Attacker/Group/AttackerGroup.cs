using JetBrains.Annotations;
using N2K;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackerGroup : PoolMember
{
    #region ___ REFERENCES ___

    // External references

    private AttackerManager _attackerManager;

    [Header("References")]

    [SerializeField]
    private LineRenderer _lineRenderer;

    #endregion ___

    #region ___ SETTINGS ___

    protected override int defaultCapacity => 5;

    protected override int maxSize => 20;

    #endregion ___

    #region ___ DATA ___

    private int _id;

    public int Id => _id;

    private AttackerGroupState _state;

    public AttackerGroupState State => _state;

    [SerializeField, ReadOnly]
    private List<Attacker> _attackerList = new();

    public int AttackerCount => _attackerList.Count;

    private Defender _targetDefender;

    public Defender TargetDefender => _targetDefender;

    // MOVEMENT

    private Vector2Int _coord;

    public Vector2Int Coord => _coord;

    private List<Int2> _path;

    public List<Int2> Path => _path;

    private int _wayPointId;

    public int WayPointId => _wayPointId;

    #endregion ___

    public void Initialize(AttackerManager attackerManager, int id, Vector2Int coord)
    {
        _id = id;
        _attackerManager = attackerManager;
        _coord = coord;
        _state = AttackerGroupState.Idle;
        _path = null;
        _targetDefender = null;

        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        DrawPath();
    }

    private void DrawPath()
    {
        if (_lineRenderer == null)
            return;

        if (_path == null || _path.Count <= 1)
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = _path.Count;

        for (int i = 0; i < _path.Count; i++)
        {
            Vector2Int coord = new(_path[i].x, _path[i].y);
            Vector3 worldPos = MapData.GetWorldPosOfCoord(coord);
            worldPos.y += 0.1f; // lift line slightly above ground
            _lineRenderer.SetPosition(i, worldPos);
        }
    }

    public void SpawnAttackers(int count)
    {
        // Check attacker set if clear or not
        if (_attackerList.Count > 0)
        {
            Debug.LogError("Attacker set need to be clear before initialized (check when attack group's despawned)");
            foreach (var attacker in _attackerList)
            {
                if (attacker != null)
                {
                    attacker.DestroySelf();
                }
            }
            _attackerList.Clear();
        }

        // Random choose data config for attacker
        AttackerTypeConfig typeConfig = _attackerManager.ConfigSO.GetRandomTypeConfig();
        AttackerWaveConfig waveConfig = typeConfig.GetWaveConfig(_attackerManager.RoundManager.CurrentWave).Value;

        // Random position to spawn
        List<Vector3> posList = _attackerManager.RoundManager.UnitFormation
            .GetUnitPosList(MapData.GetWorldPosOfCoord(_coord), count);

        // Spawn
        Attacker prefab;
        foreach (var pos in posList)
        {
            prefab = typeConfig.prefabArr.GetRandom();
            Attacker attacker = Instantiate(prefab, _attackerManager.AttackerHolder);
            attacker.Initialize(_attackerManager, this, waveConfig.health, waveConfig.goldDrop);
            attacker.transform.position = pos;
            attacker.transform.rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );
            RegisterAttacker(attacker);
        }
    }

    #region ___ ATTACKER SET ___

    public void RegisterAttacker(Attacker attacker)
    {
        if (_attackerList.Contains(attacker))
        {
            Debug.LogError($"Attacker {attacker.name}'s in group {name} already");
            return;
        }
        _attackerList.Add(attacker);

        attacker.SetGroup(this);
        // Reset id in group
        int i = 0;
        foreach (var attacker1 in _attackerList)
        {
            attacker1.SetIdInGroup(i);
            i++;
        }
    }

    public void UnregisterAttacker(Attacker attacker)
    {
        if (!_attackerList.Contains(attacker))
        {
            Debug.LogError($"Attacker {attacker.name}'s not in the group {name}");
            return;
        }
        _attackerList.Remove(attacker);
        if (_attackerList.Count <= 0)
        {
            DestroySelf();
        }

        // Reset id in group
        int i = 0;
        foreach (var attacker1 in _attackerList)
        {
            attacker1.SetIdInGroup(i);
            i++;
        }

        EventVariances.onAttackerCountUpdated?.Invoke();
    }

    public void RefreshGroupCoord()
    {
        if (_state == AttackerGroupState.Moving)
        {
            _coord = new Vector2Int(_path[_wayPointId + 1].x, _path[_wayPointId + 1].y);
        }
    }

    public void RefreshWaypointAndCombatTarget()
    {
        if (_state == AttackerGroupState.Moving)
        {
            // Process next waypoint
            _wayPointId++;
            if (_wayPointId >= _path.Count - 1)
            {
                _path = null;
                _state = AttackerGroupState.Idle;
            }
        }
        else if (_state == AttackerGroupState.Attacking)
        {
            // Check if enemy is alive
            if (_targetDefender == null || _targetDefender.State == DefenderState.None)
            {
                _targetDefender = null;
                _state = AttackerGroupState.Idle;
            }
        }
    }

    public List<Attacker> TryReleaseAttackersAtWrongCoord()
    {
        List<Attacker> list = new();

        foreach (var attacker in _attackerList.ToList())
        {
            if (attacker == null || attacker.State == AttackerState.None)
            {
                Debug.LogError("Null or destroyed attacker is still in the set");
                _attackerList.Remove(attacker);
                continue;
            }
            Vector2Int coord = MapData.GetCoordOfWorldPos(attacker.transform.position);
            if (coord != _coord)
            {
                UnregisterAttacker(attacker);
                list.Add(attacker);
            }
        }

        return list;
    }

    #endregion ___

    #region ___ STATE ___

    public void ExecuteCurrentState()
    {
        switch (_state)
        {
            case AttackerGroupState.Moving:
                foreach (var attacker in _attackerList)
                {
                    if (attacker == null || !attacker.gameObject.activeSelf)
                    {
                        Debug.LogError("attakcer is null?");
                    }
                    attacker.ChangeState(AttackerState.Moving);
                }
                break;
            case AttackerGroupState.Attacking:
                foreach (var attacker in _attackerList)
                {
                    attacker.ChangeState(AttackerState.Attacking);
                }
                break;
            case AttackerGroupState.Idle:
                foreach (var attacker in _attackerList)
                {
                    attacker.ChangeState(AttackerState.Idle);
                }
                break;
        }
    }

    public void DestroySelf()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }

        _path = null;
        _targetDefender?.SetCombatTarget(null);
        _state = AttackerGroupState.None;
        _attackerManager.RoundManager.MapData.UpdateBlockType(MapBlockType.Empty, _coord);
        _attackerManager.UnregisterAttackerGroup(this);
        ReleaseToPool();
    }

    #endregion ___

    #region ___ MOVEMENT ___

    public void SetPath(List<Int2> path)
    {
        _path = path;
        if (_path == null || _path.Count <= 1)
        {
            _path = null;
            _state = AttackerGroupState.Idle;
            return;
        }
        _wayPointId = 0;
        while (_wayPointId < _path.Count && (_path[_wayPointId].x != _coord.x || _path[_wayPointId].y != _coord.y))
        {
            _wayPointId++;
        }
        if (_wayPointId >= _path.Count - 1)
        {
            _path = null;
            _state = AttackerGroupState.Idle;
        }
        else
        {
            _state = AttackerGroupState.Moving;
        }
    }

    public Vector2Int? GetMovementTargetCoord()
    {
        if (_path == null)
        {
            return null;
        }
        else
        {
            var target = _path[_path.Count - 1];
            return new Vector2Int(target.x, target.y);
        }
    }

    #endregion ___

    #region ___ COMBAT ___

    public void SetTargetDefender(Defender defender)
    {
        _targetDefender = defender;
        defender.SetCombatTarget(this);
        _state = AttackerGroupState.Attacking;
    }

    #endregion ___
}
