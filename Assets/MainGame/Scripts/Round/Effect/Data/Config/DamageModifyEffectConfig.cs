using System;
using UnityEngine;

[Serializable]
public class DamageModifyEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.DamageModify;

    public float increaseRateNormalized;

    public override EffectConfigBase Clone()
    {
        return new DamageModifyEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
            increaseRateNormalized = increaseRateNormalized
        };
    }
}
