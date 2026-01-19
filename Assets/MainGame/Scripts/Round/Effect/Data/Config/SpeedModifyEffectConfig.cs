using System;
using UnityEngine;

[Serializable]
public class SpeedModifyEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.SpeedModify;

    public float increaseRateNormalized;

    public override EffectConfigBase Clone()
    {
        return new SpeedModifyEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
            increaseRateNormalized = increaseRateNormalized
        };
    }
}
