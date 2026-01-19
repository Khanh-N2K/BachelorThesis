using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField]
    private CardConfigSO _configSO;

    public CardConfigSO ConfigSO => _configSO;
}
