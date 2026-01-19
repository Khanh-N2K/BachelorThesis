using N2K;
using UnityEngine;

public abstract class SelectablePartBase : MonoBehaviour
{
    #region ___ REFERENCES ___
    [Header("=== SELECTABLE PART ===")]

    [Header("References")]

    [SerializeField]
    private MeshRenderer[] _meshRendererArr;
    #endregion ___

    #region ___ DATA ___
    private Material[] _originalMatArr;
    #endregion ___

    public void Initialize()
    {
        _originalMatArr = new Material[_meshRendererArr.Length];
        for (int i = 0; i < _meshRendererArr.Length; i++)
        {
            _originalMatArr[i] = _meshRendererArr[i].sharedMaterial;
        }
        SetSelectable();
    }

    public void SetSelectable(bool selectable = true)
    {
        gameObject.tag = selectable ? TagNameType.SelectablePart.ToString() : TagNameType.Untagged.ToString();
    }

    public virtual void OnMouseHovered(Material hoveredMat)
    {
        for (int i = 0; i < _meshRendererArr.Length; i++)
        {
            _meshRendererArr[i].sharedMaterial = hoveredMat;
        }
    }

    public virtual void OnMouseSelected()
    {

    }

    public virtual void OnMouseLeaved()
    {
        for (int i = 0; i < _meshRendererArr.Length; i++)
        {
            _meshRendererArr[i].sharedMaterial = _originalMatArr[i];
        }
    }
}
