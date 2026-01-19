using N2K;
using UnityEngine;

public class ObstacleSelectablePart : SelectablePartBase
{
    #region ___ REFERENCES ____
    // EXTERNAL REFERENCES 

    private Obstacle _obstacle;
    #endregion ___

    #region ___ DATA ___
    private TowerBuildingPopup _popup;
    #endregion ___

    public void Initialize(Obstacle obstacle)
    {
        _obstacle = obstacle;
    }

    public override void OnMouseSelected()
    {
        base.OnMouseSelected();
        _popup = UIManager.Instance.ShowPopup<TowerBuildingPopup>();
        _popup.SetTargetObstacle(_obstacle);
    }

    public override void OnMouseLeaved()
    {
        base.OnMouseLeaved();
        if(_popup != null)
        {
            UIManager.Instance.HidePopup(_popup);
            _popup = null;
        }
    }
}
