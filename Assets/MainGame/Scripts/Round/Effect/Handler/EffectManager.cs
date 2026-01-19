using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [SerializeField]
    private EffectInstance _effectPrefab;

    public EffectInstance EffectPrefab => _effectPrefab;
}
