using UnityEngine;

public class ParabolicBullet : BulletBase
{
    #region ___ SETTINGS ___

    [Header("=== PARABOLIC BULLET ===")]

    [Header("Settings")]

    [SerializeField]
    private Vector2 _apexHeightRange = new Vector2(1.5f, 3f);

    [SerializeField]
    private float _damageDealRange = 1f;

    #endregion

    #region ___ DATA ___

    private Vector3 _startPos;

    private Vector3 _targetPos;

    private float _duration;

    private float _apexHeight;

    #endregion

    public override void Initialize(float speed, float damage, Attacker target)
    {
        base.Initialize(speed, damage, target);
        _startPos = transform.position;
        _targetPos = target.transform.position + Vector3.up * 0.5f;
        _duration = Vector3.Distance(_startPos, _targetPos) / speed;
        __timer = 0f;
        // Random apex height
        _apexHeight = Random.Range(_apexHeightRange.x, _apexHeightRange.y);

        // Play sound
        AudioManager.Instance.PlayOneShot(AudioNameType.Tower_Barracks.ToString(), 0.5f);
    }

    private float __timer;
    float __deltaTime;
    float __height;
    private void Update()
    {
        if (target != null && Vector3.Distance(transform.position, target.transform.position) < _damageDealRange)
        {
            if (target != null && target.State != AttackerState.None && target.State != AttackerState.Inactive)
            {
                target.TakeDamage(damage);
            }
            ReleaseToPool();
            return;
        }
        if (__timer < _duration)
        {
            __deltaTime = __timer / _duration;
            Vector3 pos = Vector3.Lerp(_startPos, _targetPos, __deltaTime);
            __height = 4f * __deltaTime * (1f - __deltaTime) * _apexHeight;
            pos.y += __height;
            transform.position = pos;
            __timer += Time.deltaTime;
        }
        else
        {
            ReleaseToPool();
        }
    }
}
