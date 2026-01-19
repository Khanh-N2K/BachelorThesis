using System.Collections.Generic;
using UnityEngine;

public class CardTarget : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private List<Renderer> _rendererList;

    [SerializeField]
    private EffectHandler _effectHandler;

    #endregion ___

    #region ___ DATA ___

    private Material[] _originalMatArr;

    #endregion ___

    private void Awake()
    {
        _originalMatArr = new Material[_rendererList.Count];
        for (int i = 0; i < _rendererList.Count; i++)
        {
            _originalMatArr[i] = _rendererList[i].sharedMaterial;
        }
    }

    public void AddRenderers(Renderer[] rendererArr)
    {
        foreach (var renderer in rendererArr)
        {
            _rendererList.Add(renderer);
        }
    }

    public void ClearAllRendereres()
    {
        _rendererList.Clear();
    }

    public void Highlight(Material highlightMat)
    {
        for (int i = 0; i < _rendererList.Count; i++)
        {
            _rendererList[i].sharedMaterial = highlightMat;
        }
    }

    public void Unhighlight()
    {
        for (int i = 0; i < _rendererList.Count; i++)
        {
            _rendererList[i].sharedMaterial = _originalMatArr[i];
        }
    }

    public void ApplyEffects(EffectConfigBase[] effectArr)
    {
        foreach(var effectConfig in effectArr)
        {
            _effectHandler.AddEffect(effectConfig);
        }
    }
}
