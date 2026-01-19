using UnityEngine;

public class StraightBullet : BulletBase
{
    #region ___ SETTINGS ___

    [Header("=== STRAIGHT BULLET ===")]

    [SerializeField]
    private float _damageDealRange = 0.8f;

    #endregion ___

    #region ___ DATA ___

    private Vector3 _startPos;

    private Vector3 _targetPos;

    private float _duration;

    #endregion ___

    public override void Initialize(float speed, float damage, Attacker target)
    {
        base.Initialize(speed, damage, target);
        _startPos = transform.position;
        _targetPos = target.transform.position + Vector3.up * 0.5f;
        transform.forward = (_targetPos - transform.position).normalized;
        _duration = Vector3.Distance(_targetPos, _startPos) / speed;
        __timer = 0;

        // Play Sound
        AudioManager.Instance.PlayOneShot(AudioNameType.Tower_Archer_ShotSound.ToString(), 0.5f);
    }

    float __timer;
    private void Update()
    {
        if (target != null && Vector3.Distance(transform.position, target.transform.position) < _damageDealRange)
        {
            if (target.State != AttackerState.None && target.State != AttackerState.Inactive)
            {
                target.TakeDamage(damage);
            }
            ReleaseToPool();
            return;
        }
        if (__timer < _duration)
        {
            transform.position = Vector3.Lerp(_startPos, _targetPos, __timer / _duration);
            __timer += Time.deltaTime;
        }
        else
        {
            ReleaseToPool();
        }
    }
}
