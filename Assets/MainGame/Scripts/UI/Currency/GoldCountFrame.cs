using N2K;
using TMPro;
using UnityEngine;

public class GoldCountFrame : MonoBehaviour
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
    private Color _addMoneyColor = Color.green;

    #endregion ___

    #region ___ DATA ___

    private int _lastGoldCount;

    #endregion ___

    private void OnEnable()
    {
        _lastGoldCount = GameManager.Instance.RoundManager.GoldCount;
        _countText.text = _lastGoldCount.ToString();
        GameManager.Instance.RoundManager.onGoldChanged += OnGoldChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RoundManager.onGoldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int newAmount)
    {
        _countText.text = newAmount.ToString();
        UIManager.Instance.TryGetCurrentScreen(out IngameScreen screen);
        if (newAmount > _lastGoldCount)
        {
            TextFlyOutPopup.ShowPopupFromUIPos(_textFlyStartPos, $"+{newAmount - _lastGoldCount}"
                , screen.TextFlyHolderFront, color: _addMoneyColor, size: 30f
                , offset: new Vector2(0, Random.Range(0, 80)), duration: 1f, scale: 1f);
        }
        else if (newAmount < _lastGoldCount)
        {
            TextFlyOutPopup.ShowPopupFromUIPos(_textFlyStartPos, $"-{_lastGoldCount - newAmount}"
                , screen.TextFlyHolderFront, color: Color.red, size: 30f
                , offset: new Vector2(0, Random.Range(-80, 0)), duration: 1f, scale: 1f);
        }
        _lastGoldCount = newAmount;
    }
}
