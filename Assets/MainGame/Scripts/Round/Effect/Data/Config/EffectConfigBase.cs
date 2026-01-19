using System;
using UnityEngine;

[Serializable]
public abstract class EffectConfigBase 
{
    public abstract EffectType Type { get; }

    public float duration;
    public float triggerInterval = 1;

    public abstract EffectConfigBase Clone();
}