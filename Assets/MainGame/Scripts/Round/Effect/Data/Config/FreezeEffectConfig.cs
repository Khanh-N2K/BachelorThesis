using System;
using UnityEngine;

[Serializable]
public class FreezeEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.Freeze;

    public override EffectConfigBase Clone()
    {
        return new FreezeEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
        };
    }
}
