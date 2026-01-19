using DG.Tweening;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CardHolder : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private GameObject _cardSlotPrefab;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings - Curve")]

    [SerializeField]
    private float curveHeight = -60f;     // max Y offset

    [SerializeField]
    private float maxRotation = 15f;     // degrees

    #endregion ___

    #region ___ DATA ___

    private HashSet<Card> _cardSet = new();

    #endregion ___

    private void Awake()
    {
        foreach (Transform cardSlot in transform)
        {
            cardSlot.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EventVariances.MerchantUI.onBuyCard += OnBuyCard;
    }

    public void OnDisable()
    {
        EventVariances.MerchantUI.onBuyCard -= OnBuyCard;
    }

    private void OnBuyCard(CardConfig config, Vector2 spawnScreenPos)
    {
        SpawnCard(config, spawnScreenPos);
    }

    #region ___ CARD SET ___

    private void RegisterCard(Card card)
    {
        if (_cardSet.Contains(card))
        {
            Debug.LogError("Already in the set");
            return;
        }
        _cardSet.Add(card);

        RefreshArrangement();
    }

    public void UnregisterCard(Card card)
    {
        if (!_cardSet.Contains(card))
        {
            Debug.LogError("Not in the set");
            return;
        }
        _cardSet.Remove(card);
        card.transform.parent.gameObject.SetActive(false);
        RefreshArrangement();
    }

    #endregion ___

    public void SpawnCard(CardConfig config)
    {
        GameObject slot = GetAvailableSlot();
        Card card = slot.GetComponentInChildren<Card>();
        card.Initialize(this, config);
        RegisterCard(card);
    }

    public void SpawnCard(CardConfig config, Vector2 screenPos)
    {
        GameObject slot = GetAvailableSlot();
        Card card = slot.GetComponentInChildren<Card>();
        card.Initialize(this, config);
        RegisterCard(card);


        RectTransformUtility.ScreenPointToLocalPointInRectangle(slot.GetComponent<RectTransform>()
            , screenPos, null, out Vector2 localPoint);
        card.RectTransform.anchoredPosition = localPoint;
        card.RectTransform.DOKill();
        card.RectTransform.DOAnchorPos(card.StartAnchorPos, 0.4f);
    }

    private GameObject GetAvailableSlot()
    {
        foreach (Transform slot in transform)
        {
            if (!slot.gameObject.activeSelf)
            {
                slot.gameObject.SetActive(true);
                return slot.gameObject;
            }
        }
        GameObject slot1 = Instantiate(_cardSlotPrefab, transform);
        slot1.gameObject.SetActive(true);
        return slot1;
    }

    private void RefreshArrangement()
    {
        // Collect active slots
        List<Transform> activeSlots = new List<Transform>();
        foreach (Transform slot in transform)
        {
            if (slot.gameObject.activeSelf)
                activeSlots.Add(slot);
        }

        int activeCount = activeSlots.Count;
        if (activeCount == 0)
            return;

        // ✅ Single card case: center it, no curve
        if (activeCount == 1)
        {
            Card card = activeSlots[0].GetComponentInChildren<Card>();
            if (card != null)
            {
                card.transform.localPosition = Vector3.zero;
                card.transform.localRotation = Quaternion.identity;
            }
            return;
        }

        // ✅ Multiple cards: apply curve
        for (int i = 0; i < activeCount; i++)
        {
            Transform slot = activeSlots[i];
            Card card = slot.GetComponentInChildren<Card>();
            if (card == null) continue;

            float t = (i / (float)(activeCount - 1)) * 2f - 1f;
            float yOffset = Mathf.Abs(t) * curveHeight;
            float rotation = -t * maxRotation;

            card.transform.localPosition = new Vector3(0f, yOffset, 0f);
            card.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            card.SetStartAnchorPos(card.RectTransform.anchoredPosition);
        }
    }
}
