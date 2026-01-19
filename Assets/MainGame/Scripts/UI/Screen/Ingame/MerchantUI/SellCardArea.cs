using DG.Tweening;
using TMPro;
using UnityEngine;

public class SellCardArea : MonoBehaviour
{
    #region ___ REFERENCES ___

    [SerializeField]
    private GameObject _diamondGainObj;

    [SerializeField]
    private TMP_Text _diamondGainText;

    #endregion ___

    #region ___ DATA ___

    private Card _currentCard;

    #endregion ___

    private void Awake()
    {
        _diamondGainObj.transform.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        EventVariances.CardSystem.onStartHoveredOnSellArea += OnStartHoveredOnSellArea;
        EventVariances.CardSystem.onEndHoveredOnSellArea += OnEndHoveredOnSellArea;
        EventVariances.CardSystem.onCardReleased += OnCardReleased;
    }

    private void OnDisable()
    {
        EventVariances.CardSystem.onStartHoveredOnSellArea -= OnStartHoveredOnSellArea;
        EventVariances.CardSystem.onEndHoveredOnSellArea -= OnEndHoveredOnSellArea;
        EventVariances.CardSystem.onCardReleased -= OnCardReleased;
    }

    private void OnStartHoveredOnSellArea(Card card)
    {
        _currentCard = card;

        _diamondGainText.text = $"+{_currentCard.SellPrice.ToString()}";
        _diamondGainObj.transform.DOKill();
        _diamondGainObj.transform.DOScale(Vector3.one, 0.1f);
    }

    private void OnEndHoveredOnSellArea()
    {
        _currentCard = null;

        _diamondGainObj.SetActive(true);
        _diamondGainObj.transform.DOKill();
        _diamondGainObj.transform.DOScale(Vector3.zero, 0.1f);
    }

    private void OnCardReleased(Card card)
    {
        if (_currentCard != card || _currentCard == null)
        {
            return;
        }

        GameManager.Instance.RoundManager.AddDiamond(_currentCard.SellPrice);

        _currentCard.SetDiscared();
        OnEndHoveredOnSellArea();
    }
}
