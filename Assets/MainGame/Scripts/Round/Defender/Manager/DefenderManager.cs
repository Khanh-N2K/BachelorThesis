using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefenderManager : MonoBehaviour
{
    #region ___ REFERENCES ___
    // External References

    private RoundManager _roundManager;

    public RoundManager RoundManager => _roundManager;

    [Header("References")]

    [SerializeField]
    private DefenderSpawner _spawner;

    public DefenderSpawner Spawner => _spawner;
    #endregion ___


    #region ___ SETTINGS ___
    [Header("Settings")]

    [SerializeField]
    private DefenderConfigSO _configSO;

    public DefenderConfigSO ConfigSO => _configSO;
    #endregion ___


    #region ___ DATA ___
    private HashSet<Defender> _defenderSet = new();

    public List<Defender> DefenderList => _defenderSet.ToList();

    public int DefenderCount => _defenderSet.Count;

    private Dictionary<Vector2Int, Defender> _defenderDict = new Dictionary<Vector2Int, Defender>();

    public Action onDefenderSetUpdated;
    #endregion ___


    public void Initialize(RoundManager roundManager)
    {
        _roundManager = roundManager;
        _spawner.Initialize(this);
    }


    #region ___ DEFENDER SET ___
    public void RegisterDefender(Defender defender)
    {
        if (_defenderSet.Contains(defender))
        {
            Debug.LogError($"There's an Defender ({defender.name}) already. Make sure only register when it's empty there.");
            return;
        }
        _defenderSet.Add(defender);
        _defenderDict[defender.Coord] = defender;
        onDefenderSetUpdated?.Invoke();
    }

    public void UnregisterDefender(Defender defender)
    {
        if (!_defenderSet.Contains(defender))
        {
            Debug.LogError($"Defender ({defender.name}) is not registered in the set yet");
            return;
        }
        _defenderSet.Remove(defender);
        _defenderDict.Remove(defender.Coord);
        onDefenderSetUpdated?.Invoke();
    }

    public Defender GetDefenderAt(Vector2Int coord)
    {
        if (!_defenderDict.TryGetValue(coord, out var defender))
            return null;
        if (defender == null)
        {
            _defenderDict.Remove(coord);
            _defenderSet.Remove(defender);
            return null;
        }
        return _defenderSet.Contains(defender) ? defender : null;
    }
    #endregion ___
}
