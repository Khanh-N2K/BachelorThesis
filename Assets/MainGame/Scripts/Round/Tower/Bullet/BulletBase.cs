using DG.Tweening;
using N2K;
using UnityEngine;

public abstract class BulletBase : PoolMember
{
    #region ___ SETTINGS ___

    // Pool memeber

    protected override int defaultCapacity => 30;

    protected override int maxSize => 40;

    #endregion ___

    #region ___ DATA ___

    protected float speed;

    protected float damage;

    protected Attacker target;

    #endregion ___

    private void OnDisable()
    {
        target = null;
    }

    public virtual void Initialize(float speed, float damage, Attacker target)
    {
        this.speed = speed;
        this.damage = damage;
        this.target = target;
    }
}
