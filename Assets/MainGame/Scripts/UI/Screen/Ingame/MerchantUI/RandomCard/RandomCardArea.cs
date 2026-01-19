using Ookii.Dialogs;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RandomCardArea : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private Image _iconImg;

    [SerializeField]
    private Button _buyBtn;

    [SerializeField]
    private TMP_Text _priceText;

    #endregion ___

    #region ___

    [Header("Settings")]

    [SerializeField]
    private RandomCardConfigSO _configSO;

    #endregion ___

    #region ___ DATA ___

    private int _currentPrice;

    #endregion ___

    private void OnEnable()
    {
        RefreshVisual();
        GameManager.Instance.RoundManager.onGoldChanged += OnGoldChanged;
    }

    private void OnDisable()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onGoldChanged -= OnGoldChanged;
        }
    }

    private void Awake()
    {
        _currentPrice = _configSO.FirstPrice;
        _buyBtn.onClick.AddListener(OnClickBuyBtn);
        RefreshVisual();
    }

    public void Renew()
    {
        _currentPrice = _configSO.FirstPrice;
        RefreshVisual();
    }

    private void OnGoldChanged(int newAmount)
    {
        RefreshVisual();
    }

    private void OnClickBuyBtn()
    {
        GameManager.Instance.RoundManager.AddGold(- _currentPrice);
        _currentPrice += _configSO.PriceIncreasedAmountAfterBought;
        RefreshVisual();

        CardConfig randomConfig = GameManager.Instance.RoundManager.CardManger.ConfigSO.GetRandomConfig();
        EventVariances.MerchantUI.onBuyCard?.Invoke(randomConfig
            , RectTransformUtility.WorldToScreenPoint(null, _iconImg.transform.position));

        AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
    }

    private void RefreshVisual()
    {
        _priceText.text = _currentPrice.ToString();
        _buyBtn.interactable = GameManager.Instance.RoundManager.GoldCount >= _currentPrice;
    }
}
