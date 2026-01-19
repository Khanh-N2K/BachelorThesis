using UnityEngine;

public class CardHand : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private Animation _animation;

    [SerializeField]
    private AnimationClip _showAnim;

    [SerializeField]
    private AnimationClip _hideAnim;

    #endregion ___

    private void OnEnable()
    {
        GameManager.Instance.RoundManager.onStateChanged += OnRoundStateChanged;
        EventVariances.MerchantUI.onShowMainPanel += OnShowMerchantMainPanel;
        EventVariances.MerchantUI.onHideMainPanel += OnHideMainPanel;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onStateChanged -= OnRoundStateChanged;
        }
        EventVariances.MerchantUI.onShowMainPanel -= OnShowMerchantMainPanel;
        EventVariances.MerchantUI.onHideMainPanel -= OnHideMainPanel;
    }

    private void OnRoundStateChanged()
    {
        if (GameManager.Instance.RoundManager.State == RoundState.EndWave)
        {
            _animation.CrossFade(_hideAnim.name);
        }
        else if (GameManager.Instance.RoundManager.State == RoundState.WaveSimulating)
        {
            _animation.CrossFade(_showAnim.name);
        }
    }

    private void OnShowMerchantMainPanel()
    {
        _animation.CrossFade(_showAnim.name);
    }

    private void OnHideMainPanel()
    {
        if (GameManager.Instance.RoundManager.State != RoundState.WaveSimulating)
        {
            _animation.CrossFade(_hideAnim.name);
        }
    }
}
