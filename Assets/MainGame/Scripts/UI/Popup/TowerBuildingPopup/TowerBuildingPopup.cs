using N2K;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerBuildingPopup : PopupBase
{
    [Serializable]
    public struct TowerCardConfig
    {
        public TowerType towerType;
        public Button btn;
        public TMP_Text priceText;
    }

    #region ___ REFERENCES ___

    [Header("=== TOWER BUILDING POPUP ===")]

    [Header("References")]

    [SerializeField]
    private RectTransform _mainPanelRect;

    [SerializeField]
    private BuildTowerBtn[] _buildTowerBtnArr;

    #endregion ___

    #region ___ DATA ___

    private Obstacle _targetObstacle;

    public Obstacle TargetObstacle => _targetObstacle;

    #endregion ___

    private void Update()
    {
        if (_targetObstacle != null)
        {
            _mainPanelRect.position = GameManager.Instance.TopdownCam.Cam.WorldToScreenPoint(_targetObstacle.transform.position)
                    + Vector3.up * 100f;
        }
        else
        {
            GameManager.Instance.MouseSelector.LeaveSelectingObj();
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
        for(int i = 0; i < _buildTowerBtnArr.Length; i++)
        {
            _buildTowerBtnArr[i].Initialize(this);
        }
    }

    public void SetTargetObstacle(Obstacle target)
    {
        _targetObstacle = target;
    }
}
