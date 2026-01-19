using System.Collections.Generic;
using UnityEngine;

public class UnitFormation : MonoBehaviour
{
    #region ___ SETTINGS ___

    [Header("Settings - Zone")]

    [SerializeField]
    private Vector3 _centerWorldPos;

    [SerializeField]
    private float _squareRadius = 0.5f; // 0.5 = 1x1 Unity cell

    [Header("Settings - Unit")]

    [SerializeField, Range(1, 100)]
    private int _unitCount = 20;

    [SerializeField, Tooltip("Minimum distance between unit (visual only)")]
    private float _minSeparation = 0.1f;

    [Header("Settings - Randomness")]

    [SerializeField, Tooltip("How chaotic the distribution is"), Range(0f, 1f)]
    private float _chaos = 0.8f;

    [Header("Settings - Debug")]

    [SerializeField]
    private bool _regenerate;

    [SerializeField]
    private bool _drawGizmos = true;

    #endregion ___

    #region ___ DATA ___

    private List<Vector3> _positions = new();

    #endregion ___

    void OnValidate()
    {
        if (_regenerate)
        {
            _regenerate = false;
            GetUnitPosList(_centerWorldPos, _unitCount, _squareRadius);
        }
    }

    public List<Vector3> GetUnitPosList(Vector3 targetCenter, int unitCount, float targetSquareRadius = 0.35f)
    {
        _centerWorldPos = targetCenter;
        _unitCount = unitCount;
        _squareRadius = targetSquareRadius;
        _positions.Clear();

        int maxAttemptsPerEnemy = 20;

        for (int i = 0; i < unitCount; i++)
        {
            Vector3 pos = Vector3.zero;
            bool found = false;

            for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
            {
                pos = RandomPointInSquare();

                if (IsFarEnough(pos))
                {
                    found = true;
                    break;
                }
            }

            // If we fail spacing, still add (crowded zombie pile)
            if (!found)
                pos = RandomPointInSquare();

            _positions.Add(pos);
        }

        return _positions;
    }

    public Vector3 GetUnitPos(Vector3 center, int unitCount, int unitId, float squareRadius = 0.35f)
    {
        // Safety
        if (unitCount <= 1)
            return center;

        unitId = Mathf.Clamp(unitId, 0, unitCount - 1);

        // Decide grid resolution
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        float cellSize = (squareRadius * 2f) / gridSize;

        int xIndex = unitId % gridSize;
        int zIndex = unitId / gridSize;

        // Center grid around (0,0)
        float x = (xIndex + 0.5f) * cellSize - squareRadius;
        float z = (zIndex + 0.5f) * cellSize - squareRadius;

        // Deterministic chaos (per-id)
        Vector2 jitter = GetDeterministicJitter(unitId);
        x += jitter.x * cellSize * _chaos;
        z += jitter.y * cellSize * _chaos;

        return center + new Vector3(x, 0f, z);
    }

    private Vector2 GetDeterministicJitter(int id)
    {
        unchecked
        {
            int hash = id * 73856093;
            float x = ((hash & 0xFF) / 255f) - 0.5f;
            float y = (((hash >> 8) & 0xFF) / 255f) - 0.5f;
            return new Vector2(x, y);
        }
    }

    private Vector3 RandomPointInSquare()
    {
        float r = _squareRadius;

        // Bias randomness to avoid clean uniform look
        float x = Mathf.Lerp(
            Random.Range(-r, r),
            Random.Range(-r, r),
            _chaos
        );

        float z = Mathf.Lerp(
            Random.Range(-r, r),
            Random.Range(-r, r),
            _chaos
        );

        return _centerWorldPos + new Vector3(x, 0, z);
    }

    private bool IsFarEnough(Vector3 pos)
    {
        foreach (var p in _positions)
        {
            if (Vector3.Distance(p, pos) < _minSeparation)
                return false;
        }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos)
            return;

        // Draw square cell
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_centerWorldPos, new Vector3(_squareRadius * 2, 0.01f, _squareRadius * 2));

        // Draw zombies
        Gizmos.color = Color.green;
        foreach (var pos in _positions)
        {
            Gizmos.DrawSphere(pos, 0.05f);
        }
    }
}
