using N2K;
using TMPro;
using UnityEngine;

public class DiamondCountFrame : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private TMP_Text _countText;

    [SerializeField]
    private RectTransform _textFlyStartPos;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private Color _valueIncreasedColor = Color.green;

    #endregion ___

    #region ___ DATA ___

    private int _lastCount;

    #endregion ___

    private void OnEnable()
    {
        _lastCount = GameManager.Instance.RoundManager.GoldCount;
        _countText.text = _lastCount.ToString();
        GameManager.Instance.RoundManager.onDiamondChanged += OnDiamondChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onDiamondChanged -= OnDiamondChanged;
        }
    }

    private void OnDiamondChanged(int newAmount)
    {
        _countText.text = newAmount.ToString();
        UIManager.Instance.TryGetCurrentScreen(out IngameScreen screen);
        if (newAmount > _lastCount)
        {
            TextFlyOutPopup.ShowPopupFromUIPos(_textFlyStartPos, $"+{newAmount - _lastCount}"
                , screen.TextFlyHolderFront, color: _valueIncreasedColor, size: 30f
                , offset: new Vector2(0, Random.Range(0, 80)), duration: 1f, scale: 1f);
        }
        else if (newAmount < _lastCount)
        {
            TextFlyOutPopup.ShowPopupFromUIPos(_textFlyStartPos, $"-{_lastCount - newAmount}"
                , screen.TextFlyHolderFront, color: Color.red, size: 30f
                , offset: new Vector2(0, Random.Range(-80, 0)), duration: 1f, scale: 1f);
        }
        _lastCount = newAmount;
    }
}
