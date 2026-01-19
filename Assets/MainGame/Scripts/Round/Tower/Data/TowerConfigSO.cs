using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerConfigSO", menuName = "Scriptable Objects/Round/Tower Config")]
public class TowerConfigSO : ScriptableObject
{
    [SerializeField]
    private TowerConfig[] _towerConfigArr;

    [SerializeField]
    private float _refundRate = 0.3f;

    public TowerLevelConfig? GetTowerLevelConfig(TowerType towerType, int level)
    {
        for(int i = 0; i < _towerConfigArr.Length; i++)
        {
            if (_towerConfigArr[i].type == towerType)
            {
                for (int j = 0; j < _towerConfigArr[i].levelConfigArr.Length; j++)
                {
                    if (_towerConfigArr[i].levelConfigArr[j].level == level)
                    {
                        return _towerConfigArr[i].levelConfigArr[j];
                    }
                }
            }
        }
        return null;
    }

    public int? GetMaxLevel(TowerType towerType)
    {
        for(int i = 0; i < _towerConfigArr.Length; i++)
        {
            if (_towerConfigArr[i].type == towerType)
            {
                return _towerConfigArr[i].levelConfigArr[_towerConfigArr[i].levelConfigArr.Length - 1].level;
            }
        }
        return null;
    }

    public int? GetRefundAmount(TowerType type, int level)
    {
        bool isValidConfig = false;
        int totalPrice = 0;
        for (int i = 0; i < _towerConfigArr.Length; i++)
        {
            if (_towerConfigArr[i].type != type)
            {
                continue;
            }
            for (int j = 0; j < _towerConfigArr[i].levelConfigArr.Length; j++)
            {
                if (_towerConfigArr[i].levelConfigArr[j].level <= level)
                {
                    totalPrice += _towerConfigArr[i].levelConfigArr[j].goldPrice;
                    if (_towerConfigArr[i].levelConfigArr[j].level == level)
                    {
                        isValidConfig = true;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        if (isValidConfig)
        {
            return Mathf.CeilToInt(totalPrice * _refundRate);
        }
        else
        {
            return null;
        }
    }
}

[Serializable]
public struct TowerConfig
{
    public TowerType type;
    public TowerLevelConfig[] levelConfigArr;
}

[Serializable]
public struct TowerLevelConfig
{
    public int level;
    public int goldPrice;
    public Tower towerPrefab;
    public BulletBase bulletPrefab;
    public TowerCombatData combatData;
}

[Serializable]
public struct TowerCombatData
{
    public float range;
    public float fireRate;  // fire count per second
    public float bulletSpeed;
    public float damage;
}
