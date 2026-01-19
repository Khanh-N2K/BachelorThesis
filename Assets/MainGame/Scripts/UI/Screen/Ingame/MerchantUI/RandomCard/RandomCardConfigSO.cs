using UnityEngine;

[CreateAssetMenu(fileName = "RandomCardConfigSO", menuName = "Scriptable Objects/UI/Random Card Config")]
public class RandomCardConfigSO : ScriptableObject
{
    [SerializeField]
    private int _firstPrice = 25;

    public int FirstPrice => _firstPrice;

    [SerializeField]
    private int _priceIncreasedAmountAfterBought = 5;

    public int PriceIncreasedAmountAfterBought => _priceIncreasedAmountAfterBought;
}
