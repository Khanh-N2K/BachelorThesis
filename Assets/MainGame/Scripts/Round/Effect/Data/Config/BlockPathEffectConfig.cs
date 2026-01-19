using System;
using UnityEngine;

[Serializable]
public class BlockPathEffectConfig : EffectConfigBase
{
    public override EffectType Type => EffectType.BlockPath;

    public override EffectConfigBase Clone()
    {
        return new BlockPathEffectConfig()
        {
            duration = duration,
            triggerInterval = triggerInterval,
        };
    }
}
