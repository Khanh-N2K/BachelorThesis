using N2K;
using UnityEngine;
using UnityEngine.Pool;

public class MortarBullet : BulletBase
{
    #region ___ REFERENCES ___

    [Header("=== MORTAR BULLET ===")]

    [Header("References")]

    [SerializeField]
    private PooledVfx _explosionVfx;

    #endregion ___

    #region ___ SETTINGS ___

    [Header("Settings")]

    [SerializeField]
    private Vector2 _apexHeightRange = new Vector2(3f, 4f);

    [SerializeField]
    private float _explodeRadius = 3f;

    [SerializeField]
    private LayerMask _explodeLayers;

    #endregion ___

    #region ___ DATA ___

    private float _duration;

    private Vector3 _lastTargetPos;

    private Vector3 _bulletStartPos;

    private float _apexHeight;

    #endregion

    public override void Initialize(float speed, float damage, Attacker target)
    {
        base.Initialize(speed, damage, target);
        _bulletStartPos = transform.position;
        _duration = Vector3.Distance(_bulletStartPos, target.transform.position) / speed;
        _lastTargetPos = target.transform.position;
        __timer = 0f;
        _apexHeight = Random.Range(_apexHeightRange.x, _apexHeightRange.y);
    }

    private float __timer;
    private float __deltaTime;
    private Vector3 __pos;
    private Vector3 __forwardDir;
    private void Update()
    {
        if (target != null && target.State != AttackerState.None && target.State != AttackerState.Inactive)
        {
            _lastTargetPos = target.transform.position;
        }
        if (__timer < _duration)
        {
            __deltaTime = __timer / _duration;
            __pos = Vector3.Lerp(_bulletStartPos, _lastTargetPos, __deltaTime);
            __pos.y += 4f * __deltaTime * (1f - __deltaTime) * _apexHeight;
            __forwardDir = __pos - transform.position;
            if (__forwardDir.sqrMagnitude > 0.0001f)
            {
                transform.forward = __forwardDir;
            }
            transform.position = __pos;
            __timer += Time.deltaTime;
        }
        else
        {
            Explode();
            ReleaseToPool();
        }
    }

    private void Explode()
    {
        AudioManager.Instance.PlayOneShot(AudioNameType.Tower_Cannon.ToString(), 1.3f);
        Collider[] hitTargetArr = Physics.OverlapSphere(transform.position, _explodeRadius, _explodeLayers);
        foreach (var hit in hitTargetArr)
        {
            if (hit.CompareTag(TagNameType.Attacker.ToString()))
            {
                hit.GetComponentInParent<Attacker>()
                    .TakeDamage(damage);
            }
        }

        // Spawn vfx
        PooledVfx vfx = ObjectPoolAtlas.Instance.Get(_explosionVfx);
        vfx.transform.position = transform.position;
        vfx.transform.localScale = Vector3.one * _explodeRadius * 2;
    }
}
