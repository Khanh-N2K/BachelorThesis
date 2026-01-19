using Cysharp.Threading.Tasks;
using N2K;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AttackerManager : MonoBehaviour
{
    #region ___ REFERENCES ___

    private RoundManager _roundManager;

    public RoundManager RoundManager => _roundManager;

    private MapData _mapData => _roundManager.MapData;

    [Header("References")]

    [SerializeField]
    private AttackerSpawner _spawner;

    public AttackerSpawner Spawner => _spawner;

    [SerializeField]
    private Transform _attackerHolder;

    public Transform AttackerHolder => _attackerHolder;

    [Header("References")]

    [SerializeField]
    private PooledVfx _slowVfx;

    public PooledVfx SlowVfx => _slowVfx;

    [SerializeField]
    private PooledVfx _freezeVfx;

    public PooledVfx FreezeVfx => _freezeVfx;

    [SerializeField]
    private PooledVfx _burnVfx;

    public PooledVfx BurnVfx => _burnVfx;

    [SerializeField]
    private PooledVfx _killVfx;

    public PooledVfx KillVfx => _killVfx;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private AttackerConfigSO _configSO;

    public AttackerConfigSO ConfigSO => _configSO;

    #endregion ___

    #region ___ DATA ___

    // ATTACKER GROUP SET

    [Header("Data")]

    [SerializeField, ReadOnly]
    private List<AttackerGroup> _attackerGroupList = new();

    public int AttackerCount => _attackerGroupList.Sum(x => x.AttackerCount);

    private Dictionary<int, AttackerGroup> _attackerGroupDictById = new();

    private Dictionary<Vector2Int, AttackerGroup> _attackerGroupDictByCoord = new();

    private int _nextGroupId = 0;

    // EXECUTING STEP

    private bool _isFindingPath;

    private List<AttackerGroup> _nonCombatGroupList = new();

    private List<Defender> _nonCombatDefenderList = new();

    private List<AttackerGroup> _combatGroupList = new();

    private List<Defender> _combatDefenderList = new();

    #endregion ___

    public void Initialize(RoundManager roundManager)
    {
        _roundManager = roundManager;
        _isFindingPath = false;
        _spawner.Initialize(this);
    }

    #region ___ ATTACKER GROUP SET ___

    public void RegisterAttackerGroup(AttackerGroup attackerGroup)
    {
        if (_attackerGroupList.Contains(attackerGroup))
        {
            Debug.LogError($"There's an Attacker Group ({attackerGroup.name}) already");
            return;
        }
        _attackerGroupList.Add(attackerGroup);
        _attackerGroupDictById[attackerGroup.Id] = attackerGroup;
        _attackerGroupDictByCoord[attackerGroup.Coord] = attackerGroup;
    }

    public void UnregisterAttackerGroup(AttackerGroup attackerGroup)
    {
        if (!_attackerGroupList.Contains(attackerGroup))
        {
            Debug.LogError($"Attacker group {attackerGroup.name} is not registered in that chat yet");
            return;
        }
        _attackerGroupList.Remove(attackerGroup);
        _attackerGroupDictById.Remove(attackerGroup.Id);
        _attackerGroupDictByCoord.Remove(attackerGroup.Coord);
    }

    public int GetNewAttackerGroupId()
    {
        int value = _nextGroupId;
        _nextGroupId++;
        return value;
    }

    public AttackerGroup GetAttackerGroup(Vector2Int coord)
    {
        if (!_attackerGroupDictByCoord.TryGetValue(coord, out var attackerGroup))
        {
            return null;
        }
        if (!_attackerGroupList.Contains(attackerGroup))
        {
            Debug.LogError($"Group {attackerGroup.name} is in the coord dict but not in the set");
            return null;
        }
        return attackerGroup;
    }

    #endregion ___

    #region ___ FIND PATH ___

    public async UniTask FindPathAtStartOfWave()
    {
        await FindPath(_attackerGroupList.ToList(), _roundManager.DefenderManager.DefenderList);
    }

    private async UniTask FindPath(List<AttackerGroup> attackerGroupList, List<Defender> defenderList
        , List<Vector2Int> overrideWalkableCoordList = default, List<Vector2Int> overrideNonWalkableCoordList = default)
    {
        _isFindingPath = true;
        attackerGroupList = attackerGroupList.ToList();
        defenderList = defenderList.ToList();

        // 1) Setup scenario data
        List<ScenarioData> listScenario = new List<ScenarioData>();
        //// Try to make attacker go to it's last target
        Vector2Int? targetCoord;
        Defender defender;
        for (int i = 0; i < attackerGroupList.Count; i++)
        {
            targetCoord = attackerGroupList[i].GetMovementTargetCoord();
            if (targetCoord.HasValue && _mapData.GetBlockTypeAt(targetCoord.Value) == MapBlockType.Defender)
            {
                defender = _roundManager.DefenderManager.GetDefenderAt(targetCoord.Value);
                if (defender == null || !defenderList.Contains(defender))
                {
                    continue;
                }
                Int2 start = new Int2(attackerGroupList[i].Coord.x, attackerGroupList[i].Coord.y);
                Int2 goal = new Int2(targetCoord.Value.x, targetCoord.Value.y);
                ScenarioData scenarioData = new ScenarioData(attackerGroupList[i].Id, start, goal);
                listScenario.Add(scenarioData);
                defenderList.Remove(defender);
                attackerGroupList.RemoveAt(i);
                i--;
            }
        }
        //// Assign targets (defender or roaming)
        int roamingRadius = 10;
        while (attackerGroupList.Count > 0)
        {
            AttackerGroup group = attackerGroupList[0];
            Int2 start = new Int2(group.Coord.x, group.Coord.y);

            if (defenderList.Count > 0)
            {
                // Assign defender
                int id = Random.Range(0, defenderList.Count);
                Defender def = defenderList[id];

                Int2 goal = new Int2(def.Coord.x, def.Coord.y);
                listScenario.Add(new ScenarioData(group.Id, start, goal));

                defenderList.RemoveAt(id);
            }
            else
            {
                // Try roaming target
                Vector2Int? roamCoord = GetRandomRoamingCoord(roamingRadius);
                if (roamCoord.HasValue)
                {
                    Int2 goal = new Int2(roamCoord.Value.x, roamCoord.Value.y);
                    listScenario.Add(new ScenarioData(group.Id, start, goal));
                }
            }

            attackerGroupList.RemoveAt(0);
        }

        // 2) Prepare map matrix
        _mapData.UpdatePathFinderMatrix();
        if (overrideWalkableCoordList != null)
        {
            foreach (var walkableCoord in overrideWalkableCoordList)
            {
                PathFinder.SetCellWalkable(walkableCoord, true);
            }
        }
        if (overrideNonWalkableCoordList == null)
        {
            overrideNonWalkableCoordList = new();
        }
        for (int i = 0; i < attackerGroupList.Count; i++)
        {
            overrideNonWalkableCoordList.Add(attackerGroupList[i].Coord);
        }
        for (int i = 0; i < defenderList.Count; i++)
        {
            overrideNonWalkableCoordList.Add(defenderList[i].Coord);
        }
        foreach (var nonWalkableCoord in overrideNonWalkableCoordList)
        {
            PathFinder.SetCellWalkable(nonWalkableCoord, false);
        }

        // 3) Find path
        Dictionary<int, List<Int2>> pathDict = await PathFinder.GetPath(listScenario);

        // 4) Return if null or not success finding path
        if (pathDict == null)
        {
            foreach (var attackerGroup in attackerGroupList)
                attackerGroup.SetPath(null);
            _isFindingPath = false;
            return;
        }
        if (pathDict.Count != listScenario.Count)
        {
            Debug.LogError($"Found path for {pathDict.Count}/{listScenario.Count} attacker groups");
        }

        // 5) Assign path to attacker group
        for (int i = 0; i < pathDict.Count; i++)
        {
            int id = pathDict.ElementAt(i).Key;
            if (_attackerGroupDictById.TryGetValue(id, out AttackerGroup currentAttackerGroup))
            {
                currentAttackerGroup.SetPath(pathDict[id]);
            }
        }
        for (int i = 0; i < attackerGroupList.Count; i++)
        {
            attackerGroupList[i].SetPath(null);
        }
        _isFindingPath = false;
    }

    private Vector2Int? GetRandomRoamingCoord(int radius)
    {
        Vector2Int center = new(
            _mapData.MapSize.x / 2,
            _mapData.MapSize.y / 2
        );

        const int maxTry = 50;

        for (int i = 0; i < maxTry; i++)
        {
            int x = Random.Range(center.x - radius, center.x + radius + 1);
            int y = Random.Range(center.y - radius, center.y + radius + 1);

            if (x < 0 || y < 0 || x >= _mapData.MapSize.x || y >= _mapData.MapSize.y)
                continue;

            Vector2Int coord = new(x, y);

            if (_mapData.GetBlockTypeAt(coord) == MapBlockType.Empty)
                return coord;
        }

        return null;
    }

    #endregion ___

    #region ___ WAVE SIMULATING ___
    public async UniTask StartWaveSimulating()
    {
        await UniTask.WaitUntil(() => _isFindingPath == false);
        CommandGroups().Forget();
    }

    private async UniTask CommandGroups()
    {
        while (true)
        {
            if (IsEndWave())
            {
                _roundManager.ChangeState(RoundState.EndWave);
                break;
            }
            await PlanNextStepForGroups();
            LetGroupsExecuteNextStep();
            await UniTask.Delay(TimeSpan.FromSeconds(_roundManager.StepDuration));
            RefreshDataAfterStepExecuted();
        }
    }

    private bool IsEndWave()
    {
        return AttackerCount == 0 || _roundManager.DefenderManager.DefenderCount == 0;
    }

    private async UniTask PlanNextStepForGroups()
    {
        // If a group near a defender then it will attack that defender
        _nonCombatGroupList = _attackerGroupList.ToList();
        _nonCombatDefenderList = _roundManager.DefenderManager.DefenderList;
        _combatGroupList.Clear();
        _combatDefenderList.Clear();
        bool isHasNewGroupChangeToAttackState = false;
        foreach (AttackerGroup group in _attackerGroupList)
        {
            bool isGroupInAttackingState = false;
            if (group.State == AttackerGroupState.Attacking)
            {
                isGroupInAttackingState = true;
            }
            else if (TryMakeGroupAttackNeighborCoord(group))
            {
                isGroupInAttackingState = true;
                isHasNewGroupChangeToAttackState = true;
            }
            if (isGroupInAttackingState)
            {
                _nonCombatGroupList.Remove(group);
                _nonCombatDefenderList.Remove(group.TargetDefender);
                _combatGroupList.Add(group);
                _combatDefenderList.Add(group.TargetDefender);
            }
        }

        // Try to repath for remaining groups
        int movingGroupCount = 0;
        foreach (var group in _nonCombatGroupList)
        {
            if (group.State == AttackerGroupState.Moving)
            {
                movingGroupCount++;
            }
        }
        bool isHasGroupNotMoving = movingGroupCount < _nonCombatGroupList.Count;
        bool isHasDefenderNotBeingTargeted = movingGroupCount < _nonCombatDefenderList.Count;
        if (isHasNewGroupChangeToAttackState || (isHasGroupNotMoving && isHasDefenderNotBeingTargeted))
        {
            List<Vector2Int> nonWalkableCoordList = new();
            foreach (var group in _combatGroupList)
            {
                nonWalkableCoordList.Add(group.Coord);
            }
            foreach (var defender in _combatDefenderList)
            {
                nonWalkableCoordList.Add(defender.Coord);
            }
            await FindPath(_nonCombatGroupList, _nonCombatDefenderList, overrideNonWalkableCoordList: nonWalkableCoordList);
        }
    }

    private bool TryMakeGroupAttackNeighborCoord(AttackerGroup group)
    {
        Vector2Int neighborCoord;
        // Check nieghbor if there's defender in there to attack
        if (group.Coord.y < _mapData.MapSize.y - 1)   // Top
        {
            neighborCoord = group.Coord + Vector2Int.up;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.y > 0)    // Down
        {
            neighborCoord = group.Coord - Vector2Int.up;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.x < _mapData.MapSize.x - 1)   // Right
        {
            neighborCoord = group.Coord + Vector2Int.right;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.x > 0)    // Left
        {
            neighborCoord = group.Coord + Vector2Int.left;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.y < _mapData.MapSize.y - 1 && group.Coord.x < _mapData.MapSize.x - 1)  // Top right
        {
            neighborCoord = group.Coord + Vector2Int.one;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.y > 0 && group.Coord.x > 0)    // Down left
        {
            neighborCoord = group.Coord - Vector2Int.one;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.y < _mapData.MapSize.y - 1 && group.Coord.x > 0)  // Top left
        {
            neighborCoord = group.Coord + Vector2Int.up + Vector2Int.left;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        if (group.Coord.y > 0 && group.Coord.x < _mapData.MapSize.x - 1)  // Down right
        {
            neighborCoord = group.Coord + Vector2Int.down + Vector2Int.right;
            if (TryMakeGroupAttackDefender(group, neighborCoord))
            {
                return true;
            }
        }
        return false;
    }

    private bool TryMakeGroupAttackDefender(AttackerGroup group, Vector2Int coord)
    {
        Defender defender = _roundManager.DefenderManager.GetDefenderAt(coord);
        if (defender != null && defender.CombatTarget == null)
        {
            group.SetTargetDefender(defender);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void LetGroupsExecuteNextStep()
    {
        foreach (var group in _attackerGroupList)
        {
            group.ExecuteCurrentState();
        }
    }

    private void RefreshDataAfterStepExecuted()
    {
        // Refresh if group coord
        foreach (var group in _attackerGroupList.ToList())
        {
            group.RefreshGroupCoord();
        }
        _attackerGroupDictByCoord.Clear();
        foreach (var group in _attackerGroupList)
        {
            _attackerGroupDictByCoord[group.Coord] = group;
        }

        // Refresh waypoint and combat target
        foreach (var group in _attackerGroupList.ToList())
        {
            group.RefreshWaypointAndCombatTarget();
        }

        // Refresh attackers in group
        List<Attacker> wrongCoordAttackers = new();     // Released all attacker's in wrong place
        List<Attacker> releasedAttackerList;
        foreach (var group in _attackerGroupList.ToList())
        {
            releasedAttackerList = group.TryReleaseAttackersAtWrongCoord();
            foreach (var attacker in releasedAttackerList)
            {
                wrongCoordAttackers.Add(attacker);
            }
        }
        foreach (var attacker in wrongCoordAttackers)   // Re-assign new group for them
        {
            Vector2Int coord = MapData.GetCoordOfWorldPos(attacker.transform.position);
            AttackerGroup newGroup = GetAttackerGroup(coord);
            if (newGroup == null)
            {
                AttackerGroup group = _spawner.SpawnNewGroup(coord);
                group.RegisterAttacker(attacker);
            }
            else
            {
                newGroup.RegisterAttacker(attacker);
            }
        }

        // Refresh map data matrix
        _mapData.ReplaceAllBlockType(MapBlockType.AttackerGroup, MapBlockType.Empty);
        foreach (var group in _attackerGroupList)
        {
            _mapData.UpdateBlockType(MapBlockType.AttackerGroup, group.Coord);
        }
    }
    #endregion ___
}
