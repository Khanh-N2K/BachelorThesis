using DG.Tweening;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class MerchantUI : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private Image _backgroundImg;

    [SerializeField]
    private CanvasGroup _standeeCanvasGroup;

    [Header("References - Area")]

    [SerializeField]
    private KnownCardArea _knownCardArea;

    [SerializeField]
    private RandomCardArea _randomCardArea;

    [SerializeField]
    private SellCardArea _sellCardArea;

    [Header("References - Button")]

    [SerializeField]
    private Button _hideBtn;

    [SerializeField]
    private Button _standeeBtn;

    [Header("References - Animation")]

    [SerializeField]
    private Animation _animation;

    [SerializeField]
    private AnimationClip _showMainPanelAnim;

    [SerializeField]
    private AnimationClip _hideMainPanelAnim;

    #endregion ___

    #region ___ DATA ___

    private bool _isShowingMainPanel = false;

    #endregion ___

    private void Awake()
    {
        _standeeCanvasGroup.alpha = 0;
        _standeeCanvasGroup.blocksRaycasts = false;
        _backgroundImg.enabled = false;
    }

    private void OnEnable()
    {
        GameManager.Instance.RoundManager.onStateChanged += OnRoundStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onStateChanged -= OnRoundStateChanged;
        }
    }

    private void OnRoundStateChanged()
    {
        if (GameManager.Instance.RoundManager.State == RoundState.PlayerSetup && GameManager.Instance.RoundManager.CurrentWave > 1)
        {
            _standeeBtn.interactable = true;
            _standeeCanvasGroup.DOKill();
            _standeeCanvasGroup.blocksRaycasts = true;
            _standeeCanvasGroup.DOFade(0, 2f).
                onComplete += () => _standeeCanvasGroup.DOFade(1, 1f);
            ReNew();
        }
        else if (GameManager.Instance.RoundManager.State == RoundState.WaveSimulating)
        {
            _standeeBtn.interactable = false;
            _standeeCanvasGroup.DOKill();
            _standeeCanvasGroup.blocksRaycasts = false;
            _standeeCanvasGroup.DOFade(0f, 1f);
            OnClickHideBtn();
        }
    }

    public void ReNew()
    {
        _hideBtn.onClick.RemoveAllListeners();
        _hideBtn.onClick.AddListener(OnClickHideBtn);
        _standeeBtn.onClick.RemoveAllListeners();
        _standeeBtn.onClick.AddListener(OnClickStandeeBtn);

        _isShowingMainPanel = false;
        _backgroundImg.enabled = (false);

        _knownCardArea.Renew();
        _randomCardArea.Renew();
    }

    #region ___ BUTTONS ___

    private void OnClickHideBtn()
    {
        AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
        if (_isShowingMainPanel)
        {
            _isShowingMainPanel = false;
            _animation.Blend(_hideMainPanelAnim.name, 0.2f);
            _backgroundImg.enabled = (false);
            EventVariances.MerchantUI.onHideMainPanel?.Invoke();
        }
    }

    private void OnClickStandeeBtn()
    {
        AudioManager.Instance.PlayOneShot(AudioNameType.ClickButton.ToString(), 0.5f);
        if (!_isShowingMainPanel)
        {
            _isShowingMainPanel = true;
            _animation.Blend(_showMainPanelAnim.name, 0.2f);
            _backgroundImg.enabled = (true);
            EventVariances.MerchantUI.onShowMainPanel?.Invoke();
        }
        else
        {
            _isShowingMainPanel = false;
            _animation.Blend(_hideMainPanelAnim.name, 0.2f);
            _backgroundImg.enabled = (false);
            EventVariances.MerchantUI.onHideMainPanel?.Invoke();
        }
        EventVariances.MerchantUI.onUIInteracted?.Invoke();
    }

    #endregion ___
}
