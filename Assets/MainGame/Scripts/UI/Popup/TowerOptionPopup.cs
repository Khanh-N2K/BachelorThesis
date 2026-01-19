using N2K;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerOptionPopup : PopupBase
{
    #region ___ REFERENCES ___

    [Header("=== TOWER OPTION POPUP ===")]

    [Header("References - Title")]

    [SerializeField]
    private TMP_Text _levelText;

    [SerializeField]
    private TMP_Text _nameText;

    [Header("References - Statistics")]

    [SerializeField]
    private TMP_Text _rangeText;

    [SerializeField]
    private TMP_Text _firerateText;

    [SerializeField]
    private TMP_Text _damageText;

    [Header("References - Buttons")]

    [SerializeField]
    private Button _upgradeBtn;

    [SerializeField]
    private TMP_Text _upgradePriceText;

    [SerializeField]
    private Button _sellBtn;

    [SerializeField]
    private TMP_Text _refundAmountText;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private Color _clickableColor = Color.white;

    [SerializeField]
    private Color _nonClickableColor = Color.red;

    #endregion ___

    #region ___ DATA ___

    private Tower _target;

    private int _upgradePrice;

    private int _refundAmount;

    #endregion ___

    private void OnEnable()
    {
        RefreshBtnInteractable();
        GameManager.Instance.RoundManager.onGoldChanged += OnGoldChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onGoldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int newGold)
    {
        RefreshBtnInteractable();
    }

    protected override void Initialize()
    {
        base.Initialize();
        _upgradeBtn.onClick.AddListener(OnClickUpgradeBtn);
        _sellBtn.onClick.AddListener(OnClickSellBtn);
    }

    private void OnClickUpgradeBtn()
    {
        GameManager.Instance.RoundManager.AddGold(-_upgradePrice);
        Obstacle groundObstacle = _target.GroundObstacle;
        _target.DestroySelf();
        GameManager.Instance.MouseSelector.LeaveSelectingObj();
        Tower tower = GameManager.Instance.RoundManager.TowerManager
            .PlaceNewTowerOnObstacle(_target.Type, _target.Config.level + 1, groundObstacle);
        _target = tower;
        GameManager.Instance.MouseSelector.SelectObj(_target.SelectablePart);
    }

    private void OnClickSellBtn()
    {
        GameManager.Instance.RoundManager.AddGold(_refundAmount);
        GameManager.Instance.MouseSelector.LeaveSelectingObj();
        _target.DestroySelf();
    }

    public void SetTargetTower(Tower target)
    {
        _target = target;
        bool canUpgrade = _target.Config.level 
            < GameManager.Instance.RoundManager.TowerManager.ConfigSO.GetMaxLevel(_target.Type).Value;
        if (canUpgrade)
        {
            _upgradePrice = GameManager.Instance.RoundManager.TowerManager.ConfigSO
                    .GetTowerLevelConfig(_target.Type, _target.Config.level + 1).Value.goldPrice;
            _upgradeBtn.gameObject.SetActive(true);
        }
        else
        {
            _upgradeBtn.gameObject.SetActive(false);
        }
        _refundAmount = GameManager.Instance.RoundManager.TowerManager.ConfigSO
            .GetRefundAmount(_target.Type, _target.Config.level).Value;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        _levelText.text = _target.Config.level.ToString();
        _nameText.text = _target.Type.ToString();
        _rangeText.text = $"Range: {_target.Config.combatData.range.ToString()}";
        _firerateText.text = $"Fire rate: {_target.Config.combatData.fireRate.ToString()}";
        _damageText.text = $"Damage: {_target.Config.combatData.damage.ToString()}";
        // Btn
        if (_upgradeBtn.gameObject.activeSelf)
        {
            _upgradePriceText.text = _upgradePrice.ToString();
        }
        RefreshBtnInteractable();
        _refundAmountText.text = $"+{_refundAmount}";
    }

    private void RefreshBtnInteractable()
    {
        if (!_upgradeBtn.gameObject.activeSelf)
        {
            return;
        }
        if (GameManager.Instance.RoundManager.GoldCount >= _upgradePrice)
        {
            _upgradeBtn.interactable = true;
            _upgradePriceText.color = _clickableColor;
        }
        else
        {
            _upgradeBtn.interactable = false;
            _upgradePriceText.color = _nonClickableColor;
        }
    }
}
