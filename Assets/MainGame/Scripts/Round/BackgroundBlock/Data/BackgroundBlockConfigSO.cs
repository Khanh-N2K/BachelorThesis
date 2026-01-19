using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundBlockConfigSO", menuName = "Scriptable Objects/Round/Background Block Config")]
public class BackgroundBlockConfigSO : ScriptableObject
{
    [Header("Floor block")]

    [SerializeField]
    private GameObject[] _floorPrefabArr;

    public GameObject RandomFloorPrefab => _floorPrefabArr.GetRandom();

    [Header("Border block")]

    [SerializeField]
    private GameObject[] _borderPrefabArr;

    public GameObject RandomBorderPrefab => _borderPrefabArr.GetRandom();

    [Header("Obstacle")]

    [SerializeField]
    private Obstacle[] _obstaclePrefabArr;

    public Obstacle RandomObstacle => _obstaclePrefabArr.GetRandom();
}