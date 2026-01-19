using System;
using UnityEngine;

[Serializable]
public class DamageOverTimeEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.DamageOverTime;

    public float dps;

    public override EffectConfigBase Clone()
    {
        return new DamageOverTimeEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
            dps = dps
        };
    }
}
