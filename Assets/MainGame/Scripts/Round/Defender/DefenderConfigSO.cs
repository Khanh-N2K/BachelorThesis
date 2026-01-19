using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefenderConfigSO", menuName = "Scriptable Objects/Round/Defender Config")]
public class DefenderConfigSO : ScriptableObject
{
    [SerializeField]
    private Defender[] _prefabArr;

    public Defender GetRandomPrefab()
    {
        return _prefabArr[Random.Range(0, _prefabArr.Length)];
    }
}
