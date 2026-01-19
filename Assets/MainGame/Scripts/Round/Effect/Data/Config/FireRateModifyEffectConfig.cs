using System;
using UnityEngine;

[Serializable]
public class FireRateModifyEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.FireRateModify;

    public float increaseRateNormalized;

    public override EffectConfigBase Clone()
    {
        return new FireRateModifyEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
            increaseRateNormalized = increaseRateNormalized
        };
    }
}
