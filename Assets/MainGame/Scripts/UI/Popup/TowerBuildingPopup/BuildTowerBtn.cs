using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildTowerBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region ___ REFRENCES ___

    // External references

    private TowerBuildingPopup _buildingPopup;

    [Header("References")]

    [SerializeField]
    private Button _btn;

    [SerializeField]
    private TMP_Text _priceText;

    [Header("References - Animation")]

    [SerializeField]
    private Animation _animation;

    [SerializeField]
    private AnimationClip _hoveredAnim;

    [SerializeField]
    private AnimationClip _nonHoveredAnim;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private TowerType _towerType;

    [SerializeField]
    private Color _clickableColor = Color.white;

    [SerializeField]
    private Color _nonClickableColor = Color.red;

    #endregion ___

    #region ___ DATA ___

    private int _price;

    #endregion ___

    private void OnEnable()
    {
        RefreshBtnClickable();
        GameManager.Instance.RoundManager.onGoldChanged += OnGoldChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onGoldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int newValue)
    {
        RefreshBtnClickable();
        if (GameManager.Instance.RoundManager.GoldCount < _price)
        {
            _animation.CrossFade(_nonHoveredAnim.name);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_btn.interactable)
        {
            _animation.CrossFade(_hoveredAnim.name);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _animation.CrossFade(_nonHoveredAnim.name, 0);
    }

    public void Initialize(TowerBuildingPopup buildingPopup)
    {
        _buildingPopup = buildingPopup;
        _btn.onClick.AddListener(() => OnClickTowerCardBtn());
        _price = GameManager.Instance.RoundManager.TowerManager.ConfigSO
            .GetTowerLevelConfig(_towerType, 1).Value.goldPrice;
        _priceText.text = _price.ToString();
        RefreshBtnClickable();
    }

    private void OnClickTowerCardBtn()
    {
        GameManager.Instance.RoundManager.AddGold(-_price);
        GameManager.Instance.MouseSelector.LeaveSelectingObj();
        Tower tower = GameManager.Instance.RoundManager.TowerManager.PlaceNewTowerOnObstacle(_towerType, 1, _buildingPopup.TargetObstacle);
        GameManager.Instance.MouseSelector.SelectObj(tower.SelectablePart);
    }

    private void RefreshBtnClickable()
    {
        if (GameManager.Instance.RoundManager.GoldCount >= _price)
        {
            _btn.interactable = true;
            _priceText.color = _clickableColor;
        }
        else
        {
            _btn.interactable = false;
            _priceText.color = _nonClickableColor;
        }
    }
}
