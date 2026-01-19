using N2K;
using UnityEngine;

public class PooledVfx : PoolMember
{
    #region ___ REFERENCES ___

    [Header("References")]

    [SerializeField]
    private ParticleSystem _particleSystem;

    #endregion ___

    #region ___ SETTINGS ___

    // Pool member

    protected override int defaultCapacity => 10;

    protected override int maxSize => 20;

    [Header("Settings")]

    [SerializeField]
    private float _timeOut = 3;

    #endregion ___

    private void OnEnable()
    {
        _particleSystem.Play(true);
        timer = 0;
    }

    private float timer = 0;
    private void Update()
    {
        if (timer > _timeOut)
        {
            ReleaseToPool();
            return;
        }
        timer += Time.deltaTime;
    }

    public void SetTimeOut(float timeOut)
    {
        _timeOut = timeOut;
    }
}
