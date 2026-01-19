using N2K;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectHandler : MonoBehaviour
{
    private HashSet<EffectInstance> _effectSet = new();

    public HashSet<EffectInstance> EffectSet => _effectSet;

    public Action<EffectInstance> onEffectStarted;

    public Action<EffectInstance> onEffectTriggered;

    public Action<EffectInstance> onEffectEnded;

    public void AddEffect(EffectConfigBase config)
    {
        EffectInstance effect = ObjectPoolAtlas.Instance.Get(GameManager.Instance.RoundManager.EffectManager.EffectPrefab);
        effect.gameObject.name = config.Type.ToString();
        effect.transform.parent = transform; 
        effect.Initialize(this, config);
        RegisterEffect(effect);
        onEffectStarted?.Invoke(effect);
    }

    public void RemoveEffect(EffectInstance effect)
    {
        UnregisterEffect(effect);
        onEffectEnded?.Invoke(effect);
    }

    private void RegisterEffect(EffectInstance effect)
    {
        if (_effectSet.Contains(effect))
        {
            Debug.LogError($"Effect {effect.name} is in the set already");
            return;
        }
        _effectSet.Add(effect);
    }

    private void UnregisterEffect(EffectInstance effect)
    {
        if (!_effectSet.Contains(effect))
        {
            Debug.LogError($"Effect {effect.Type} is not in the set to remove");
            return;
        }
        _effectSet.Remove(effect);
    }
}
