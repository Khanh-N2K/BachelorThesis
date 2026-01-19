using System.Collections.Generic;
using UnityEngine;

public class KnownCardArea : MonoBehaviour
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private List<KnownCard> _cardList;

    #endregion ___

    public void Renew()
    {
        foreach(var card in _cardList)
        {
            CardConfig config = GameManager.Instance.RoundManager.CardManger.ConfigSO.GetRandomConfig();
            card.Initialzie(config);
            card.gameObject.SetActive(true);
        }
    }
}
