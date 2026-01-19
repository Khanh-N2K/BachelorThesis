using UnityEngine;

[CreateAssetMenu(fileName = "RoundConfigSO", menuName = "Scriptable Objects/Round/Round Config")]
public class RoundConfigSO : ScriptableObject
{
    [SerializeField]
    private int _totalWave = 10;

    public int TotalWave => _totalWave;

    [SerializeField]
    private int _startGold = 100;

    public int StartGold => _startGold;

    [SerializeField]
    private int _startDiamond = 0;

    public int StartDiamond => _startDiamond;

    [SerializeField]
    private float _stepDuration = 2;

    public float StepDuration => _stepDuration;
}
