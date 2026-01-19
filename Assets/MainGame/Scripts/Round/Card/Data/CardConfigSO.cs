using N2K;
using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CardConfigSO", menuName = "Scriptable Objects/Round/Card Config")]
public class CardConfigSO : ScriptableObject
{
    [SerializeField]
    private CardConfig[] _configArr;

    public CardConfig[] ConfigArr => (CardConfig[]) _configArr.Clone();

    public CardConfig GetRandomConfig()
    {
        return _configArr.GetRandom();
    }

    public CardConfig GetFirstConfig(EffectType effectType)
    {
        if (_configArr == null)
            return null;

        foreach (var config in _configArr)
        {
            if (config == null || config._effectArr == null)
                continue;

            foreach (var effect in config._effectArr)
            {
                if (effect != null && effect.Type == effectType)
                {
                    return config;
                }
            }
        }

        return null;
    }
}

[Serializable]
public class CardConfig
{
    public string name;
    
    public string description;

    public Sprite icon;

    public int buyPrice;

    public int sellPrice;
    
    public TagNameType[] targetTagArr;

    public float dragEffectRadius;

    [SerializeReference, SubclassSelector]
    public EffectConfigBase[] _effectArr;
}
