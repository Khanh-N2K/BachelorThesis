using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "AttackerConfigSO", menuName = "Scriptable Objects/Round/Attacker Config")]
public class AttackerConfigSO : ScriptableObject
{
    [SerializeField]
    private AttackerTypeConfig[] _typeConfigArr;

    [SerializeField]
    private AttackerSpawnConfig[] _spawnConfigArr;

    #region ___ ATTACK TYPE ___

    public AttackerTypeConfig GetRandomTypeConfig()
    {
        return _typeConfigArr[Random.Range(0, _typeConfigArr.Length)];
    }

    public Attacker GetRandomPrefab()
    {
        int id = Random.Range(0, _typeConfigArr.Length);
        return _typeConfigArr[id].prefabArr[Random.Range(0, _typeConfigArr[id].prefabArr.Length)];
    }

    public Attacker GetRandomPrefab(AttackerType attackerType)
    {
        for (int i = 0; i < _typeConfigArr.Length; i++)
        {
            if (_typeConfigArr[i].attackerType != attackerType)
            {
                continue;
            }
            return _typeConfigArr[i].prefabArr[Random.Range(0, _typeConfigArr[i].prefabArr.Length)];
        }
        return null;
    }

    #endregion ___

    #region ___ SPAWN CONFIG ___

    public AttackerSpawnConfig? GetSpawnConfig(int wave)
    {
        for(int i = 0; i < _spawnConfigArr.Length; i++)
        {
            if (_spawnConfigArr[i].wave == wave)
            {
                return _spawnConfigArr[i];
            }
        }
        return null;
    }

    #endregion ___
}

[Serializable]
public class AttackerTypeConfig
{
    public AttackerType attackerType;
    public Attacker[] prefabArr;
    public AttackerWaveConfig[] waveConfigArr;

    public AttackerWaveConfig? GetWaveConfig(int waveId)
    {
        for (int i = 0; i < waveConfigArr.Length; i++)
        {
            if (waveConfigArr[i].waveId == waveId)
            {
                return waveConfigArr[i];
            }
        }
        return null;
    }
}

[Serializable]
public struct AttackerWaveConfig
{
    public int waveId;
    public float health;
    public int goldDrop;
}

[Serializable]
public struct AttackerSpawnConfig
{
    public int wave;
    public int attackerCount;
    public int spawnPosCount;
}