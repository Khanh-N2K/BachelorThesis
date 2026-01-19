using N2K;
using UnityEngine;

public class TowerSelectablePart : SelectablePartBase
{
    #region ___ REFERENCES ___
    private Tower _tower;
    #endregion ___

    #region ___ DATA ___
    private TowerOptionPopup _popup;
    #endregion ___

    public void Initialize(Tower tower)
    {
        _tower = tower;
    }

    public override void OnMouseSelected()
    {
        base.OnMouseSelected();
        _popup = UIManager.Instance.ShowPopup<TowerOptionPopup>();
        _popup.SetTargetTower(_tower);
        _tower.FireRange.SetShowRange(true);
    }

    public override void OnMouseLeaved()
    {
        base.OnMouseLeaved();
        if (_popup != null)
        {
            UIManager.Instance.HidePopup(_popup);
            _popup = null;
        }
        _tower.FireRange.SetShowRange(false);
    }
}
