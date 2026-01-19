using UnityEngine;

public class HomingBullet : BulletBase
{
    #region ___ SETTINGS ___

    [Header("=== HOMMING BULLET ===")]

    [SerializeField]
    private float _damageDealRange = 0.5f;

    #endregion

    #region ___ DATA ___

    private Vector3 _lastTarget;

    #endregion

    public override void Initialize(float speed, float damage, Attacker target)
    {
        base.Initialize(speed, damage, target);
        _lastTarget = target.transform.position;
        AudioManager.Instance.PlayOneShot(AudioNameType.Tower_Mage_FireSound.ToString(), 0.5f);
    }

    Vector3 __dir;
    private void Update()
    {
        if(target != null && target.State != AttackerState.None && target.State != AttackerState.Inactive)
        {
            if(Vector3.Distance(transform.position, target.transform.position) < _damageDealRange)
            {
                target.TakeDamage(damage);
                ReleaseToPool();
                return;
            }
            __dir = (target.transform.position - transform.position).normalized;
            transform.position += __dir * speed * Time.deltaTime;
            transform.forward = __dir;
            _lastTarget = target.transform.position;
        }
        else    // Move to last target
        {
            if (Vector3.Distance(transform.position, _lastTarget) < _damageDealRange)
            {
                ReleaseToPool();
                return;
            }
            __dir = (_lastTarget - transform.position).normalized;
            transform.position += __dir * speed * Time.deltaTime;
            transform.forward = __dir;
        }
    }
}
