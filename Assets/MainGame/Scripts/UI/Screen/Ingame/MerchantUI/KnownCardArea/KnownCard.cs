using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KnownCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private Image _iconImg;

    [SerializeField]
    private Button _buyBtn;

    [SerializeField]
    private TMP_Text _priceText;

    [SerializeField]
    private TMP_Text _descriptionText;

    #endregion ___

    #region ___ DATA ___

    private CardConfig _config;

    #endregion ___

    private void Awake()
    {
        _descriptionText.transform.parent.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.Instance.RoundManager.onDiamondChanged += OnDiamondChanged;
        RefreshVisual();
    }

    private void OnDisable()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onDiamondChanged -= OnDiamondChanged;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _descriptionText.transform.parent.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _descriptionText.transform.parent.gameObject.SetActive(false);
    }

    private void OnDiamondChanged(int newAmount)
    {
        RefreshVisual();
    }

    public void Initialzie(CardConfig config)
    {
        _config = config;

        _buyBtn.onClick.RemoveAllListeners();
        _buyBtn.onClick.AddListener(OnClickBuyBtn);

        _iconImg.sprite = config.icon;
        _priceText.text = config.buyPrice.ToString();
        _descriptionText.text = config.description;
        RefreshVisual();
    }

    private void OnClickBuyBtn()
    {
        GameManager.Instance.RoundManager.AddDiamond(-_config.buyPrice);
        gameObject.SetActive(false);
        EventVariances.MerchantUI.onBuyCard?.Invoke(_config, RectTransformUtility.WorldToScreenPoint(null, transform.position));

        AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
    }

    private void RefreshVisual()
    {
        if (_config == null)
        {
            return;
        }

        _buyBtn.interactable = _config.buyPrice <= GameManager.Instance.RoundManager.DiamondCount;
    }
}
