using System;
using UnityEngine;

[Serializable]
public class KillEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.Kill;

    public float ceilThreshold_HealthRateNormalized;

    public override EffectConfigBase Clone()
    {
        return new KillEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
            ceilThreshold_HealthRateNormalized = ceilThreshold_HealthRateNormalized
        };
    }
}
